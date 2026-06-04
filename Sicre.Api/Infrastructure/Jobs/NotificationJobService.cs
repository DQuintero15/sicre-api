using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.Notifications.Dtos;
using Sicre.Api.Features.Notifications.Services;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;
using Sicre.Api.Shared.Email;

namespace Sicre.Api.Infrastructure.Jobs;

public interface INotificationJobService
{
    Task RunDailyNotificationsAsync();
}

public class NotificationJobService(
    ApplicationDbContext db,
    IEmailService emailService,
    IEmailTemplateService emailTemplateService,
    IDateHelper dateHelper,
    ILogger<NotificationJobService> logger,
    UserManager<User> userManager,
    INotificationRealtimeService realtimeService
) : INotificationJobService
{
    public async Task RunDailyNotificationsAsync()
    {
        logger.LogInformation("NotificationJob iniciado.");

        try
        {
            var settings = await db.SICRESettings.FirstOrDefaultAsync();
            if (settings is null || !settings.AutoNotify)
            {
                logger.LogInformation("AutoNotify desactivado. NotificationJob omitido.");
                return;
            }

            var today = DateOnly.FromDateTime(dateHelper.GetCurrentDateTime());

            var activeInstances = await db
                .ReportInstances.Include(i => i.Report)
                    .ThenInclude(r => r!.ControlEntity)
                .Include(i => i.Report)
                    .ThenInclude(r => r!.Branch)
                .Include(i => i.Report)
                    .ThenInclude(r => r!.SenderResponsibleUser)
                .Include(i => i.ResponsibleUser)
                .Include(i => i.SupervisorUser)
                .Where(i => i.Status == ReportStatus.Pending || i.Status == ReportStatus.Overdue)
                .ToListAsync();

            logger.LogInformation(
                "NotificationJob: {Count} instancias activas a evaluar.",
                activeInstances.Count
            );

            int alertsSent = 0;

            foreach (var instance in activeInstances)
            {
                if (instance.Report is null)
                    continue;

                var daysUntilDue = instance.DueDate.DayNumber - today.DayNumber;

                string? alertType = null;

                if (daysUntilDue < 0 || instance.Status == ReportStatus.Overdue)
                {
                    alertType = "Crítica";
                }
                else if (daysUntilDue <= instance.Report.AlertCriticalDays)
                {
                    alertType = "Riesgo";
                }
                else if (daysUntilDue <= instance.Report.AlertFollowUpDays)
                {
                    alertType = "Seguimiento";
                }
                else if (daysUntilDue <= instance.Report.AlertEarlyDays)
                {
                    alertType = "Preventiva";
                }

                if (alertType is null)
                    continue;

                await SendAlertAsync(instance, alertType, daysUntilDue);
                alertsSent++;
            }

            logger.LogInformation(
                "NotificationJob completado. Alertas enviadas: {Count}.",
                alertsSent
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error crítico en NotificationJob.");
            throw;
        }
    }

    private async Task SendAlertAsync(ReportInstance instance, string alertType, int daysUntilDue)
    {
        try
        {
            var report = instance.Report!;
            var entityName = report.ControlEntity?.Name ?? string.Empty;
            var entityAbbr = report.ControlEntity?.Abbreviation;
            var legalBasis = string.IsNullOrWhiteSpace(report.LegalBasis)
                ? "la normativa vigente"
                : report.LegalBasis;
            var branchName = report.Branch?.Name;

            var title = BuildTitle(alertType, report.Code, entityAbbr ?? entityName);
            var content = BuildContent(
                alertType,
                report.Name,
                instance.DueDate,
                legalBasis,
                entityName
            );

            var severity = GetSeverity(alertType);
            var priority = GetPriority(severity);

            var usersToNotify = CollectKnownUsers(instance, alertType);

            // Resolve NotificationEmails → additional users + email-only addresses
            var includeExtraEmails = alertType is "Seguimiento" or "Riesgo" or "Crítica";
            var emailOnlySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (includeExtraEmails)
            {
                var extraEmails = ParseEmails(report.NotificationEmails);
                if (extraEmails.Count > 0)
                {
                    var emailList = extraEmails.ToList();
                    var matchedUsers = await userManager
                        .Users.Where(u => u.Email != null && emailList.Contains(u.Email.ToLower()))
                        .ToListAsync();

                    foreach (var u in matchedUsers)
                        usersToNotify[u.Id] = u;

                    var matchedEmails = matchedUsers
                        .Where(u => u.Email != null)
                        .Select(u => u.Email!.ToLowerInvariant())
                        .ToHashSet();

                    foreach (var e in extraEmails)
                    {
                        if (!matchedEmails.Contains(e))
                            emailOnlySet.Add(e);
                    }
                }
            }

            // Notify registered users (APP notification + email)
            var notifiedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var user in usersToNotify.Values)
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                    continue;

                var notification = await PersistNotificationAsync(
                    instance,
                    user.Id,
                    title,
                    content,
                    severity,
                    priority
                );

                var body = emailTemplateService.GetReportAlertNotificationEmailTemplate(
                    $"{user.FirstName} {user.LastName}",
                    report.Name,
                    instance.PeriodName,
                    instance.DueDate,
                    alertType,
                    content,
                    instance.Id,
                    notification?.Id ?? Guid.Empty,
                    branchName
                );

                await emailService.SendEmailAsync(user.Email!, title, body);
                notifiedEmails.Add(user.Email);

                logger.LogInformation(
                    "NotificationJob: alerta {AlertType} enviada a {Email} — reporte {Code}, período {Period}.",
                    alertType,
                    user.Email,
                    report.Code,
                    instance.PeriodName
                );
            }

            // Notify email-only addresses not already covered
            foreach (var email in emailOnlySet)
            {
                if (notifiedEmails.Contains(email))
                    continue;

                var body = emailTemplateService.GetReportAlertNotificationEmailTemplate(
                    "Usuario",
                    report.Name,
                    instance.PeriodName,
                    instance.DueDate,
                    alertType,
                    content,
                    instance.Id,
                    Guid.Empty, // correo adicional sin notificación APP — pixel no aplica
                    branchName
                );

                await emailService.SendEmailAsync(email, title, body);

                logger.LogInformation(
                    "NotificationJob: alerta {AlertType} enviada a correo adicional {Email} — reporte {Code}.",
                    alertType,
                    email,
                    report.Code
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "NotificationJob: error enviando alerta {AlertType} para instancia {InstanceId}.",
                alertType,
                instance.Id
            );
        }
    }

    // Returns the known User objects to notify based on alert level.
    // Preventiva: EntityUploadResponsible + SenderResponsible
    // Seguimiento/Riesgo/Crítica: + FollowUpLeader (SupervisorUser)
    private static Dictionary<Guid, User> CollectKnownUsers(
        ReportInstance instance,
        string alertType
    )
    {
        var users = new Dictionary<Guid, User>();

        if (instance.ResponsibleUser is { } responsible)
            users[responsible.Id] = responsible;

        if (instance.Report?.SenderResponsibleUser is { } sender)
            users[sender.Id] = sender;

        if (alertType is "Seguimiento" or "Riesgo" or "Crítica")
        {
            if (instance.SupervisorUser is { } supervisor)
                users[supervisor.Id] = supervisor;
        }

        return users;
    }

    private static string BuildTitle(string alertType, string reportCode, string entityRef) =>
        $"[{entityRef}] Reporte {reportCode} — {alertType}";

    private static string BuildContent(
        string alertType,
        string reportName,
        DateOnly dueDate,
        string legalBasis,
        string entityName
    ) =>
        alertType switch
        {
            "Preventiva" =>
                $"Recordatorio: El reporte '{reportName}' tiene fecha de vencimiento {dueDate:dd/MM/yyyy}. "
                    + $"Recordar que el envío oportuno de esta información evita sanciones por incumplimiento de acuerdo {legalBasis}.",
            "Seguimiento" =>
                $"Aviso falta de reporte: El reporte '{reportName}' aún no ha sido reportado, "
                    + $"su vencimiento próximo es {dueDate:dd/MM/yyyy}. "
                    + $"Recordar que el envío oportuno de esta información evita sanciones por incumplimiento de acuerdo {legalBasis}.",
            "Riesgo" =>
                $"Aviso falta de reporte: El reporte '{reportName}' vence hoy y no ha sido reportado. "
                    + $"Recordar que el envío oportuno de esta información evita sanciones por incumplimiento de acuerdo {legalBasis}.",
            "Crítica" => $"Aviso Incumplimiento reporte '{reportName}' — {entityName}: "
                + $"El reporte '{reportName}' no fue reportado oportunamente, "
                + $"eso expone a la compañía a las sanciones según la {legalBasis}.",
            _ => $"El reporte '{reportName}' requiere atención. Vencimiento: {dueDate:dd/MM/yyyy}.",
        };

    private static NotificationSeverity GetSeverity(string alertType) =>
        alertType switch
        {
            "Preventiva" => NotificationSeverity.Info,
            "Seguimiento" => NotificationSeverity.Warning,
            "Riesgo" => NotificationSeverity.Urgent,
            "Crítica" => NotificationSeverity.Critical,
            _ => NotificationSeverity.General,
        };

    private static NotificationPriority GetPriority(NotificationSeverity severity) =>
        severity switch
        {
            NotificationSeverity.Critical => NotificationPriority.Critical,
            NotificationSeverity.Urgent => NotificationPriority.High,
            NotificationSeverity.Warning => NotificationPriority.Normal,
            _ => NotificationPriority.Low,
        };

    private static HashSet<string> ParseEmails(string? value)
    {
        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(value))
            return emails;

        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(value);
            if (parsed != null)
            {
                foreach (var e in parsed)
                    AddEmail(emails, e);
                return emails;
            }
        }
        catch { }

        foreach (
            var e in value.Split(
                [',', ';', '\n', '\r'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
        )
            AddEmail(emails, e);

        return emails;
    }

    private static void AddEmail(ISet<string> emails, string? email)
    {
        if (!string.IsNullOrWhiteSpace(email))
            emails.Add(email.Trim().ToLowerInvariant());
    }

    private async Task<Notification?> PersistNotificationAsync(
        ReportInstance instance,
        Guid userId,
        string title,
        string content,
        NotificationSeverity severity,
        NotificationPriority priority
    )
    {
        try
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Content = content,
                Type = NotificationType.APP,
                Severity = severity,
                Priority = priority,
                ReportInstanceId = instance.Id,
                Url = $"/report-instances/{instance.Id}",
                CreatedAt = DateTime.UtcNow,
            };

            db.Notifications.Add(notification);
            await db.SaveChangesAsync();

            await realtimeService.PublishCreatedAsync(
                new NotificationDto
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    Content = notification.Content,
                    Type = notification.Type,
                    Severity = notification.Severity,
                    Priority = notification.Priority,
                    Readed = false,
                    CreatedAt = notification.CreatedAt,
                    ReportInstanceId = notification.ReportInstanceId,
                    Url = notification.Url,
                },
                userId
            );

            return notification;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "NotificationJob: error creando notificación APP para usuario {UserId}, instancia {InstanceId}.",
                userId,
                instance.Id
            );
            return null;
        }
    }
}
