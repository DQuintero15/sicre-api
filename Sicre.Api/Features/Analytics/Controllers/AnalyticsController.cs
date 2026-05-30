using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Features.Analytics.DTOs;
using Sicre.Api.Features.Analytics.Services;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;
using Sicre.Api.Shared.Reports;

namespace Sicre.Api.Features.Analytics.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class AnalyticsController(
    IAnalyticsService analyticsService,
    MonthlyReportPdfGenerator pdfGenerator,
    IWebHostEnvironment env
) : BaseController
{
    [HttpGet("state-distribution")]
    public async Task<ActionResult<ApiResponse<StateDistributionDto>>> GetStateDistribution(
        [FromQuery] AnalyticsFilterRequest filter
    )
    {
        var result = await analyticsService.GetStateDistributionAsync(
            GetUserId(),
            GetUserRoles(),
            filter
        );
        return FromResult(result);
    }

    [HttpGet("compliance-trend")]
    public async Task<ActionResult<ApiResponse<List<ComplianceTrendDto>>>> GetComplianceTrend(
        [FromQuery] AnalyticsFilterRequest filter
    )
    {
        var result = await analyticsService.GetComplianceTrendAsync(
            GetUserId(),
            GetUserRoles(),
            filter
        );
        return FromResult(result);
    }

    [HttpGet("compliance-by-entity")]
    public async Task<ActionResult<ApiResponse<List<EntityComplianceDto>>>> GetComplianceByEntity(
        [FromQuery] AnalyticsFilterRequest filter
    )
    {
        var result = await analyticsService.GetComplianceByEntityAsync(
            GetUserId(),
            GetUserRoles(),
            filter
        );
        return FromResult(result);
    }

    [HttpGet("compliance-by-responsible")]
    public async Task<
        ActionResult<ApiResponse<List<ResponsibleComplianceDto>>>
    > GetComplianceByResponsible([FromQuery] AnalyticsFilterRequest filter)
    {
        var result = await analyticsService.GetComplianceByResponsibleAsync(
            GetUserId(),
            GetUserRoles(),
            filter
        );
        return FromResult(result);
    }

    [HttpGet("monthly-metrics")]
    public async Task<ActionResult<ApiResponse<MonthlyInstancesMetricsDto>>> GetMonthlyMetrics(
        [FromQuery] int month,
        [FromQuery] int year
    )
    {
        if (month < 1 || month > 12)
            return BadRequest(
                ApiResponse<MonthlyInstancesMetricsDto>.Fail(
                    System.Net.HttpStatusCode.BadRequest,
                    "El mes debe estar entre 1 y 12."
                )
            );
        if (year < 2000 || year > 2100)
            return BadRequest(
                ApiResponse<MonthlyInstancesMetricsDto>.Fail(
                    System.Net.HttpStatusCode.BadRequest,
                    "El año es inválido."
                )
            );

        var result = await analyticsService.GetMonthlyInstancesMetricsAsync(
            GetUserId(),
            GetUserRoles(),
            month,
            year
        );
        return FromResult(result);
    }

    [HttpGet("compliance-by-branch")]
    public async Task<ActionResult<ApiResponse<List<BranchComplianceDto>>>> GetComplianceByBranch(
        [FromQuery] AnalyticsFilterRequest filter
    )
    {
        var result = await analyticsService.GetComplianceByBranchAsync(
            GetUserId(),
            GetUserRoles(),
            filter
        );
        return FromResult(result);
    }

    [HttpGet("export-pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportPdf([FromQuery] AnalyticsFilterRequest filter)
    {
        var userId = GetUserId();
        var userRoles = GetUserRoles() ?? [];

        var (distTask, trendTask, entityTask, responsibleTask, branchTask) = (
            analyticsService.GetStateDistributionAsync(userId, userRoles, filter),
            analyticsService.GetComplianceTrendAsync(userId, userRoles, filter),
            analyticsService.GetComplianceByEntityAsync(userId, userRoles, filter),
            analyticsService.GetComplianceByResponsibleAsync(userId, userRoles, filter),
            analyticsService.GetComplianceByBranchAsync(userId, userRoles, filter)
        );

        await Task.WhenAll(distTask, trendTask, entityTask, responsibleTask, branchTask);

        var dist = distTask.Result.Data ?? new StateDistributionDto();
        var trend = trendTask.Result.Data ?? [];
        var entities = entityTask.Result.Data ?? [];
        var responsible = responsibleTask.Result.Data ?? [];
        var branches = branchTask.Result.Data ?? [];

        var now = DateTime.UtcNow;
        var dateRangeLabel = filter.StartDate.HasValue && filter.EndDate.HasValue
            ? $"{filter.StartDate:dd/MM/yyyy} - {filter.EndDate:dd/MM/yyyy}"
            : filter.StartDate.HasValue
                ? $"Desde {filter.StartDate:dd/MM/yyyy}"
                : filter.EndDate.HasValue
                    ? $"Hasta {filter.EndDate:dd/MM/yyyy}"
                    : "Reporte General";

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
            LogoLlanogas = TryReadLogoFile(Path.Combine(env.ContentRootPath, "Assets", "Images", "logo-llanogas.webp")),
            LogoCusianagas = TryReadLogoFile(Path.Combine(env.ContentRootPath, "Assets", "Images", "logo-cusianogas.webp")),
        };

        var pdfBytes = pdfGenerator.Generate(reportData);
        return File(pdfBytes, "application/pdf", $"analitica-sicre-{now:yyyy-MM-dd}.pdf");
    }

    private static byte[]? TryReadLogoFile(string path)
    {
        try { return System.IO.File.Exists(path) ? System.IO.File.ReadAllBytes(path) : null; }
        catch { return null; }
    }

    private List<string> GetUserRoles() =>
        User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
}
