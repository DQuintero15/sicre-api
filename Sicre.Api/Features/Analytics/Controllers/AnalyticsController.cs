using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Features.Analytics.DTOs;
using Sicre.Api.Features.Analytics.Services;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Analytics.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class AnalyticsController(IAnalyticsService analyticsService) : BaseController
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

    private List<string> GetUserRoles() =>
        User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
}
