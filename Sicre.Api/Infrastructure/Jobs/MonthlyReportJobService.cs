using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;
using Sicre.Api.Shared.Email;

namespace Sicre.Api.Infrastructure.Jobs;

public interface IMonthlyReportJobService
{
    Task RunAsync();
}

public class MonthlyReportJobService(
    ApplicationDbContext db,
    IEmailService emailService,
    IEmailTemplateService emailTemplateService,
    IDateHelper dateHelper,
    ILogger<MonthlyReportJobService> logger
) : IMonthlyReportJobService
{
    static MonthlyReportJobService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task RunAsync()
    {
        logger.LogInformation("MonthlyReportJob iniciado.");

        try
        {
            var now = dateHelper.GetCurrentDateTime();
            // Job runs on 1st of month — report covers the previous month
            var target = now.AddMonths(-1);
            var targetYear = target.Year;
            var targetMonth = target.Month;
            var periodKey = $"{targetYear}-{targetMonth:D2}";

            // Idempotency: skip if already processed for this period
            var alreadySent = await db.AuditEvents.AnyAsync(e =>
                e.EntityType == "MonthlyReport" &&
                e.Action == AuditAction.MonthlyReportSent &&
                e.MetadataJson == periodKey
            );

            if (alreadySent)
            {
                logger.LogInformation(
                    "MonthlyReportJob: período {Period} ya procesado, omitido.", periodKey
                );
                return;
            }

            // Query instances for the target period
            var instances = await db.ReportInstances
                .Include(i => i.Report).ThenInclude(r => r!.ControlEntity)
                .Include(i => i.Report).ThenInclude(r => r!.Branch)
                .Include(i => i.ResponsibleUser)
                .Where(i => i.PeriodYear == targetYear && i.PeriodMonth == targetMonth)
                .OrderBy(i => i.Report!.Code)
                .ToListAsync();

            // Get recipients: Administrator + ComplianceSupervisor
            var roleIds = await db.Roles
                .Where(r =>
                    r.Name == nameof(Role.Administrator) ||
                    r.Name == nameof(Role.ComplianceSupervisor))
                .Select(r => r.Id)
                .ToListAsync();

            var recipientIds = await db.UserRoles
                .Where(ur => roleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            var recipients = await db.Users
                .Where(u => recipientIds.Contains(u.Id) && u.Email != null && u.IsActive)
                .ToListAsync();

            if (recipients.Count == 0)
            {
                logger.LogWarning(
                    "MonthlyReportJob: sin destinatarios activos para {Period}.", periodKey
                );
                return;
            }

            var culture = new CultureInfo("es-CO");
            var monthName = culture.TextInfo.ToTitleCase(
                culture.DateTimeFormat.GetMonthName(targetMonth)
            );

            var pdfBytes = instances.Count > 0
                ? GeneratePdf(instances, monthName, targetYear)
                : null;

            var subject = $"Reporte Mensual de Estado — {monthName} {targetYear}";
            var attachments = pdfBytes is not null
                ? new List<EmailAttachment>
                {
                    new()
                    {
                        FileName = $"reporte-mensual-{periodKey}.pdf",
                        Content = pdfBytes,
                        ContentType = "application/pdf",
                    },
                }
                : null;

            int emailsSent = 0;
            foreach (var user in recipients)
            {
                try
                {
                    var body = emailTemplateService.GetMonthlyStatusEmailTemplate(
                        $"{user.FirstName} {user.LastName}",
                        $"{monthName} {targetYear}"
                    );
                    await emailService.SendEmailAsync(
                        user.Email!, subject, body, attachments: attachments
                    );
                    emailsSent++;
                    logger.LogInformation(
                        "MonthlyReportJob: email enviado a {Email}.", user.Email
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex, "MonthlyReportJob: error enviando a {Email}.", user.Email
                    );
                }
            }

            // Record idempotency audit event (use first recipient as system performer)
            db.AuditEvents.Add(new AuditEvent
            {
                EntityType = "MonthlyReport",
                EntityId = Guid.Empty,
                Action = AuditAction.MonthlyReportSent,
                PerformedByUserId = recipients[0].Id,
                MetadataJson = periodKey,
                PerformedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            logger.LogInformation(
                "MonthlyReportJob completado. Emails: {Count}, período: {Period}.",
                emailsSent, periodKey
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error crítico en MonthlyReportJob.");
            throw;
        }
    }

    private static byte[] GeneratePdf(
        List<ReportInstance> instances,
        string monthName,
        int year
    )
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header()
                    .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                    .PaddingBottom(8)
                    .Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Reporte Mensual de Estado")
                                .FontSize(16).Bold().FontColor(Color.FromHex("#1d3e81"));
                            col.Item().Text($"{monthName} {year}")
                                .FontSize(11).FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(120).AlignRight().Column(col =>
                        {
                            col.Item().Text("SICRE").FontSize(12).Bold();
                            col.Item().Text("Grupo del Llano")
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });

                page.Content().PaddingTop(12).Column(col =>
                {
                    var total      = instances.Count;
                    var sentOnTime = instances.Count(r => r.Status == ReportStatus.SentOnTime);
                    var sentLate   = instances.Count(r => r.Status == ReportStatus.SentLate);
                    var pending    = instances.Count(r => r.Status == ReportStatus.Pending);
                    var overdue    = instances.Count(r => r.Status == ReportStatus.Overdue);

                    col.Item().PaddingBottom(12).Row(row =>
                    {
                        AddSummaryCell(row, "Total",       total.ToString(),      "#1d3e81");
                        AddSummaryCell(row, "A tiempo",    sentOnTime.ToString(), "#16a34a");
                        AddSummaryCell(row, "Con retraso", sentLate.ToString(),   "#d97706");
                        AddSummaryCell(row, "Pendientes",  pending.ToString(),    "#6b7280");
                        AddSummaryCell(row, "Vencidos",    overdue.ToString(),    "#dc2626");
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(60);    // Código
                            cols.RelativeColumn(3);     // Nombre
                            cols.RelativeColumn(2);     // Entidad
                            cols.RelativeColumn(1.5f);  // Sede
                            cols.ConstantColumn(80);    // Vencimiento
                            cols.ConstantColumn(80);    // Estado
                            cols.RelativeColumn(2);     // Responsable
                        });

                        table.Header(header =>
                        {
                            static IContainer HeaderCell(IContainer c) =>
                                c.Background(Color.FromHex("#1d3e81")).Padding(5);

                            header.Cell().Element(HeaderCell)
                                .Text("Código").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderCell)
                                .Text("Reporte").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderCell)
                                .Text("Entidad").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderCell)
                                .Text("Sede").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderCell)
                                .Text("Vencimiento").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderCell)
                                .Text("Estado").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderCell)
                                .Text("Responsable").FontColor(Colors.White).Bold();
                        });

                        var alt = false;
                        foreach (var inst in instances)
                        {
                            var bg = alt ? Color.FromHex("#f0f4ff") : Colors.White;
                            alt = !alt;

                            IContainer RowCell(IContainer c) =>
                                c.Background(bg).BorderBottom(1)
                                 .BorderColor(Colors.Grey.Lighten3).Padding(4);

                            var (statusText, statusColor) = inst.Status switch
                            {
                                ReportStatus.SentOnTime => ("A tiempo",    "#16a34a"),
                                ReportStatus.SentLate   => ("Con retraso", "#d97706"),
                                ReportStatus.Overdue    => ("Vencido",     "#dc2626"),
                                ReportStatus.Pending    => ("Pendiente",   "#6b7280"),
                                _                       => (inst.Status.ToString(), "#333333"),
                            };

                            table.Cell().Element(RowCell)
                                .Text(inst.Report?.Code ?? "—");
                            table.Cell().Element(RowCell)
                                .Text(inst.Report?.Name ?? "—");
                            table.Cell().Element(RowCell)
                                .Text(inst.Report?.ControlEntity?.Name ?? "—");
                            table.Cell().Element(RowCell)
                                .Text(inst.Report?.Branch?.Name ?? "—");
                            table.Cell().Element(RowCell)
                                .Text(inst.DueDate.ToString("dd/MM/yyyy"));
                            table.Cell().Element(RowCell)
                                .Text(statusText)
                                .FontColor(Color.FromHex(statusColor)).Bold();
                            table.Cell().Element(RowCell)
                                .Text(inst.ResponsibleUser is { } u
                                    ? $"{u.FirstName} {u.LastName}" : "—");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span($"SICRE — {monthName} {year}  ·  Página ")
                        .FontColor(Colors.Grey.Medium);
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void AddSummaryCell(
        RowDescriptor row, string label, string value, string hexColor
    )
    {
        row.RelativeItem()
           .Border(1).BorderColor(Colors.Grey.Lighten2)
           .Padding(8).Column(col =>
           {
               col.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Medium);
               col.Item().Text(value).FontSize(18).Bold()
                  .FontColor(Color.FromHex(hexColor));
           });
    }
}
