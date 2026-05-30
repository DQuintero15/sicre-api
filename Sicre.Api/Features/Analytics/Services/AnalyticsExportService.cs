using Sicre.Api.Features.Analytics.DTOs;
using Sicre.Api.Shared.Reports;

namespace Sicre.Api.Features.Analytics.Services;

public sealed class AnalyticsExportService(
    IAnalyticsService analyticsService,
    MonthlyReportPdfGenerator pdfGenerator,
    IWebHostEnvironment env
) : IAnalyticsExportService
{
    public async Task<ExportPdfResult> ExportAsync(
        AnalyticsFilterRequest filter,
        Guid userId,
        IList<string> userRoles
    )
    {
        var dist = (await analyticsService.GetStateDistributionAsync(userId, userRoles, filter)).Data
            ?? new StateDistributionDto();
        var trend = (await analyticsService.GetComplianceTrendAsync(userId, userRoles, filter)).Data
            ?? [];
        var entities = (await analyticsService.GetComplianceByEntityAsync(userId, userRoles, filter)).Data
            ?? [];
        var responsible = (await analyticsService.GetComplianceByResponsibleAsync(userId, userRoles, filter)).Data
            ?? [];
        var branches = (await analyticsService.GetComplianceByBranchAsync(userId, userRoles, filter)).Data
            ?? [];

        var now = DateTime.UtcNow;
        var dateRangeLabel = BuildDateRangeLabel(filter);
        var dateSuffix = BuildDateSuffix(filter, now);

        var reportData = new MonthlyReportData
        {
            PeriodLabel = $"Analitica de Reportes — {dateRangeLabel}",
            PeriodYear = now.Year,
            PeriodMonth = now.Month,
            GeneratedAt = $"Generado el {now:dd/MM/yyyy HH:mm}",
            StateDistribution = dist,
            Trend = trend,
            ByEntity = entities,
            ByResponsible = responsible,
            ByBranch = branches,
            LogoLlanogas = TryReadLogoFile("logo-llanogas.webp"),
            LogoCusianagas = TryReadLogoFile("logo-cusianogas.webp"),
        };

        var pdfBytes = pdfGenerator.Generate(reportData);
        var fileName = $"SICRE_Informe-Cumplimiento_{dateSuffix}.pdf";

        return new ExportPdfResult(pdfBytes, fileName);
    }

    private static string BuildDateRangeLabel(AnalyticsFilterRequest filter) =>
        filter.StartDate.HasValue && filter.EndDate.HasValue
            ? $"{filter.StartDate:dd/MM/yyyy} - {filter.EndDate:dd/MM/yyyy}"
            : filter.StartDate.HasValue
                ? $"Desde {filter.StartDate:dd/MM/yyyy}"
                : filter.EndDate.HasValue
                    ? $"Hasta {filter.EndDate:dd/MM/yyyy}"
                    : "Reporte General";

    private static string BuildDateSuffix(AnalyticsFilterRequest filter, DateTime now) =>
        filter.StartDate.HasValue && filter.EndDate.HasValue
            ? $"{filter.StartDate:yyyyMMdd}-{filter.EndDate:yyyyMMdd}"
            : $"{now:yyyy-MM-dd}";

    private byte[]? TryReadLogoFile(string fileName)
    {
        try
        {
            var path = Path.Combine(env.ContentRootPath, "Assets", "Images", fileName);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
        catch
        {
            return null;
        }
    }
}
