using Microsoft.AspNetCore.Identity;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.Analytics.DTOs;
using Sicre.Api.Features.Analytics.Services;
using Sicre.Api.Shared;
using Sicre.Api.Shared.Email;
using Sicre.Api.Shared.Reports;

namespace Sicre.Api.Infrastructure.Jobs;

public interface IMonthlyReportJobService
{
    Task RunAsync();
}

public class MonthlyReportJobService(
    IAnalyticsService analyticsService,
    IEmailService emailService,
    IEmailTemplateService emailTemplateService,
    MonthlyReportPdfGenerator pdfGenerator,
    IWebHostEnvironment env,
    IDateHelper dateHelper,
    UserManager<User> userManager,
    ILogger<MonthlyReportJobService> logger
) : IMonthlyReportJobService
{
    private static readonly IList<string> AdminRoles = [nameof(Role.Administrator)];

    public async Task RunAsync()
    {
        logger.LogInformation("MonthlyReportJob iniciado.");

        try
        {
            var (prevYear, prevMonth) = GetPreviousMonth();
            var periodLabel = $"{SpanishMonthName(prevMonth)} {prevYear}";

            logger.LogInformation("MonthlyReportJob: generando informe para {Period}.", periodLabel);

            // ─── Fetch analytics data ─────────────────────────────────
            var startDate  = new DateOnly(prevYear, prevMonth, 1);
            var endDate    = new DateOnly(prevYear, prevMonth, DateTime.DaysInMonth(prevYear, prevMonth));
            var trendStart = startDate.AddMonths(-11);

            var monthFilter = new AnalyticsFilterRequest { StartDate = startDate, EndDate = endDate };
            var trendFilter = new AnalyticsFilterRequest { StartDate = trendStart, EndDate = endDate };

            var systemId = Guid.Empty;

            var (distTask, trendTask, entityTask, responsibleTask, branchTask) = (
                analyticsService.GetStateDistributionAsync(systemId, AdminRoles, monthFilter),
                analyticsService.GetComplianceTrendAsync(systemId, AdminRoles, trendFilter),
                analyticsService.GetComplianceByEntityAsync(systemId, AdminRoles, monthFilter),
                analyticsService.GetComplianceByResponsibleAsync(systemId, AdminRoles, monthFilter),
                analyticsService.GetComplianceByBranchAsync(systemId, AdminRoles, monthFilter)
            );

            await Task.WhenAll(distTask, trendTask, entityTask, responsibleTask, branchTask);

            var dist        = distTask.Result.Data ?? new StateDistributionDto();
            var trend       = trendTask.Result.Data ?? [];
            var entities    = entityTask.Result.Data ?? [];
            var responsible = responsibleTask.Result.Data ?? [];
            var branches    = branchTask.Result.Data ?? [];

            // ─── Load logos ───────────────────────────────────────────
            var assetsPath      = Path.Combine(env.ContentRootPath, "Assets", "Images");
            var logoLlanogas    = TryReadFile(Path.Combine(assetsPath, "logo-llanogas.webp"));
            var logoCusianagas  = TryReadFile(Path.Combine(assetsPath, "logo-cusianagas.webp"));

            // ─── Generate PDF ─────────────────────────────────────────
            var now = dateHelper.GetCurrentDateTime();
            var reportData = new MonthlyReportData
            {
                PeriodLabel       = periodLabel,
                PeriodYear        = prevYear,
                PeriodMonth       = prevMonth,
                GeneratedAt       = $"Generado el {now.Day} de {SpanishMonthName(now.Month)} de {now.Year}",
                StateDistribution = dist,
                Trend             = trend,
                ByEntity          = entities,
                ByResponsible     = responsible,
                ByBranch          = branches,
                LogoLlanogas      = logoLlanogas,
                LogoCusianagas    = logoCusianagas,
            };

            var pdfBytes = pdfGenerator.Generate(reportData);
            logger.LogInformation("MonthlyReportJob: PDF generado ({Bytes} bytes).", pdfBytes.Length);

            // ─── Get recipients ───────────────────────────────────────
            var adminUsers      = await userManager.GetUsersInRoleAsync(nameof(Role.Administrator));
            var supervisorUsers = await userManager.GetUsersInRoleAsync(nameof(Role.ComplianceSupervisor));

            var recipients = adminUsers
                .Concat(supervisorUsers)
                .DistinctBy(u => u.Id)
                .Where(u => !string.IsNullOrWhiteSpace(u.Email))
                .ToList();

            logger.LogInformation("MonthlyReportJob: {Count} destinatarios.", recipients.Count);

            // ─── Send emails ──────────────────────────────────────────
            var subject     = $"SICRE — Informe Mensual de Cumplimiento · {periodLabel}";
            var attachment  = new EmailAttachment
            {
                FileName    = $"Informe_Cumplimiento_{prevYear}_{prevMonth:D2}.pdf",
                Content     = pdfBytes,
                ContentType = "application/pdf",
            };

            int sent = 0;
            foreach (var user in recipients)
            {
                var body = emailTemplateService.GetMonthlyReportEmailTemplate(periodLabel);
                var ok   = await emailService.SendEmailAsync(user.Email!, subject, body, true, [attachment]);
                if (ok) sent++;
            }

            logger.LogInformation("MonthlyReportJob completado. Emails enviados: {Sent}/{Total}.", sent, recipients.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MonthlyReportJob: error crítico.");
            throw;
        }
    }

    // ─── Private helpers ──────────────────────────────────────────────────

    private (int year, int month) GetPreviousMonth()
    {
        var now = dateHelper.GetCurrentDateTime();
        var first = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
        return (first.Year, first.Month);
    }

    private static byte[]? TryReadFile(string path)
    {
        try { return File.Exists(path) ? File.ReadAllBytes(path) : null; }
        catch { return null; }
    }

    private static string SpanishMonthName(int month) => month switch
    {
        1  => "Enero",
        2  => "Febrero",
        3  => "Marzo",
        4  => "Abril",
        5  => "Mayo",
        6  => "Junio",
        7  => "Julio",
        8  => "Agosto",
        9  => "Septiembre",
        10 => "Octubre",
        11 => "Noviembre",
        12 => "Diciembre",
        _  => month.ToString(),
    };
}
