using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.Notifications.Dtos;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared.Email;

namespace Sicre.Api.Features.Notifications.Services;

public interface INotificationAlertService
{
    Task NotifyInstanceEventAsync(
        Guid instanceId,
        string eventType,
        Guid triggeredByUserId,
        CancellationToken ct = default
    );
}

public class NotificationAlertService(
    ApplicationDbContext db,
    IEmailService emailService,
    IEmailTemplateService emailTemplateService,
    INotificationRealtimeService realtimeService,
    ILogger<NotificationAlertService> logger
) : INotificationAlertService
{
    public async Task NotifyInstanceEventAsync(
        Guid instanceId,
        string eventType,
        Guid triggeredByUserId,
        CancellationToken ct = default
    )
    {
        try
        {
            var settings = await db.SICRESettings.FirstOrDefaultAsync(ct);
            var autoNotify = settings?.AutoNotify ?? false;

            var instance = await db
                .ReportInstances.Include(i => i.Report)
                    .ThenInclude(r => r!.ControlEntity)
                .Include(i => i.ResponsibleUser)
                .Include(i => i.SupervisorUser)
                .FirstOrDefaultAsync(i => i.Id == instanceId, ct);

            if (instance?.Report is null)
                return;

            var triggerUser = await db.Users.FindAsync([triggeredByUserId], ct);
            var triggerName = triggerUser is not null
                ? $"{triggerUser.FirstName} {triggerUser.LastName}"
                : "Un usuario";

            var (title, content, severity) = BuildEventMessage(eventType, triggerName, instance);

            var priority = severity switch
            {
                NotificationSeverity.Critical => NotificationPriority.Critical,
                NotificationSeverity.Urgent => NotificationPriority.High,
                NotificationSeverity.Warning => NotificationPriority.Normal,
                _ => NotificationPriority.Low,
            };

            var recipients = CollectRecipients(eventType, instance, triggeredByUserId);

            // ─── Notify registered users ───────────────────────────────
            foreach (var user in recipients.Values)
            {
                await NotifyUserAsync(
                    user,
                    title,
                    content,
                    severity,
                    priority,
                    instance,
                    eventType,
                    autoNotify,
                    ct
                );
            }

            // ─── Notify subscribers from NotificationEmails ────────────
            var emailSubscribers = GetEmailSubscribers(
                instance,
                recipients.Keys,
                triggeredByUserId
            );
            foreach (var email in emailSubscribers)
            {
                if (!autoNotify)
                    continue;

                var emailBody = emailTemplateService.GetInstanceEventEmailTemplate(
                    email,
                    eventType,
                    title,
                    content,
                    instance.Id
                );
                await emailService.SendEmailAsync(email, title, emailBody);

                logger.LogInformation(
                    "Notificación de evento {EventType} enviada por email a suscriptor {Email} para instancia {InstanceId}.",
                    eventType,
                    email,
                    instanceId
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error enviando notificaciones de evento {EventType} para instancia {InstanceId}.",
                eventType,
                instanceId
            );
        }
    }

    private static (string title, string content, NotificationSeverity severity) BuildEventMessage(
        string eventType,
        string triggerName,
        ReportInstance instance
    )
    {
        var reportCode = instance.Report?.Code ?? string.Empty;
        var reportName = instance.Report?.Name ?? "Reporte";
        var periodName = instance.PeriodName;
        var entityAbbr =
            instance.Report?.ControlEntity?.Abbreviation
            ?? instance.Report?.ControlEntity?.Name
            ?? string.Empty;
        var prefix = string.IsNullOrWhiteSpace(entityAbbr) ? string.Empty : $"[{entityAbbr}] ";

        return eventType switch
        {
            "Delivered" => (
                $"{prefix}{reportCode} — Reporte enviado",
                $"{triggerName} marcó el reporte '{reportName}' (período {periodName}) como enviado.",
                NotificationSeverity.Info
            ),
            "Reverted" => (
                $"{prefix}{reportCode} — Reporte revertido",
                $"{triggerName} revirtió el envío del reporte '{reportName}' (período {periodName}). La instancia volvió a estado pendiente.",
                NotificationSeverity.Warning
            ),
            "DeadlineExtended" => (
                $"{prefix}{reportCode} — Fecha límite modificada",
                $"{triggerName} modificó la fecha límite del reporte '{reportName}' (período {periodName}).",
                NotificationSeverity.Info
            ),
            "AttachmentUploaded" => (
                $"{prefix}{reportCode} — Adjunto cargado",
                $"{triggerName} subió un adjunto al reporte '{reportName}' (período {periodName}).",
                NotificationSeverity.General
            ),
            "NoteAdded" => (
                $"{prefix}{reportCode} — Nueva nota",
                $"{triggerName} publicó una nota en el reporte '{reportName}' (período {periodName}).",
                NotificationSeverity.General
            ),
            _ => (
                $"{prefix}{reportCode} — Evento en reporte",
                $"Nuevo evento en el reporte '{reportName}' (período {periodName}).",
                NotificationSeverity.General
            ),
        };
    }

    private Dictionary<Guid, User> CollectRecipients(
        string eventType,
        ReportInstance instance,
        Guid triggeredByUserId
    )
    {
        var users = new Dictionary<Guid, User>();

        void Add(User? user)
        {
            if (user is not null && user.Id != triggeredByUserId)
                users[user.Id] = user;
        }

        switch (eventType)
        {
            case "Delivered":
            case "AttachmentUploaded":
                Add(instance.SupervisorUser);
                break;

            case "Reverted":
            case "DeadlineExtended":
                Add(instance.SupervisorUser);
                Add(instance.ResponsibleUser);
                break;

            case "NoteAdded":
                Add(instance.ResponsibleUser);
                break;
        }

        // Add registered users whose email matches Report.NotificationEmails
        if (!string.IsNullOrWhiteSpace(instance.Report?.NotificationEmails))
        {
            try
            {
                var emails = System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                    instance.Report.NotificationEmails
                );
                if (emails is not null)
                {
                    var matchedUsers = db
                        .Users.Where(u => emails.Contains(u.Email!) && u.Id != triggeredByUserId)
                        .ToList();

                    foreach (var user in matchedUsers)
                    {
                        if (!users.ContainsKey(user.Id))
                            users[user.Id] = user;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Error al parsear NotificationEmails del reporte {ReportId}",
                    instance.Report?.Id
                );
            }
        }

        return users;
    }

    private List<string> GetEmailSubscribers(
        ReportInstance instance,
        Dictionary<Guid, User>.KeyCollection alreadyNotifiedUserIds,
        Guid triggeredByUserId
    )
    {
        var emailOnly = new List<string>();

        if (string.IsNullOrWhiteSpace(instance.Report?.NotificationEmails))
            return emailOnly;

        try
        {
            var emails = System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                instance.Report.NotificationEmails
            );
            if (emails is null)
                return emailOnly;

            var registeredEmails = db
                .Users.Where(u => emails.Contains(u.Email!))
                .Select(u => u.Email!)
                .ToHashSet();

            foreach (var email in emails)
            {
                if (string.IsNullOrWhiteSpace(email))
                    continue;

                // Skip if this email belongs to a user who was already notified
                if (registeredEmails.Contains(email))
                    continue;

                emailOnly.Add(email);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Error al procesar NotificationEmails del reporte {ReportId}",
                instance.Report?.Id
            );
        }

        return emailOnly;
    }

    private async Task NotifyUserAsync(
        User user,
        string title,
        string content,
        NotificationSeverity severity,
        NotificationPriority priority,
        ReportInstance instance,
        string eventType,
        bool autoNotify,
        CancellationToken ct
    )
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
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
        await db.SaveChangesAsync(ct);

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
            user.Id
        );

        if (autoNotify && !string.IsNullOrWhiteSpace(user.Email))
        {
            var emailBody = emailTemplateService.GetInstanceEventEmailTemplate(
                $"{user.FirstName} {user.LastName}",
                eventType,
                title,
                content,
                instance.Id
            );
            await emailService.SendEmailAsync(user.Email!, title, emailBody);
        }

        logger.LogInformation(
            "Notificación de evento {EventType} enviada a usuario {UserId} para instancia {InstanceId}.",
            eventType,
            user.Id,
            instance.Id
        );
    }
}
