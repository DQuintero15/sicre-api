using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Features.ReportInstances.Dtos.Requests;
using Sicre.Api.Features.ReportInstances.Dtos.Responses;
using Sicre.Api.Features.ReportInstances.Services;
using Sicre.Api.Features.Reports.Services;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.ReportInstances.Controllers;

[ApiController]
[Route("api/report-instances")]
[Authorize]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class ReportInstancesController(IReportInstanceService reportInstanceService)
    : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ReportInstanceSummaryResponse>>>> GetAll(
        [FromQuery] GetReportInstancesRequest request,
        CancellationToken ct
    )
    {
        var result = await reportInstanceService.GetAllAsync(request, ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReportInstanceResponse>>> GetById(
        Guid id,
        CancellationToken ct
    )
    {
        var result = await reportInstanceService.GetByIdAsync(id, ct);
        return FromResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReportInstanceResponse>>> Create(
        [FromBody] CreateManualReportInstanceRequest request,
        CancellationToken ct
    )
    {
        var userId = GetUserId();
        var result = await reportInstanceService.CreateManualAsync(request, userId, ct);
        if (result.Success)
            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
        return FromResult(result);
    }

    [HttpGet("preview/{reportId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReportInstanceCandidate>>>> GetPreview(
        Guid reportId,
        CancellationToken ct
    )
    {
        var result = await reportInstanceService.GetPreviewAsync(reportId, ct);
        return FromResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReportInstanceResponse>>> Update(
        Guid id,
        [FromBody] UpdateReportInstanceRequest request,
        CancellationToken ct
    )
    {
        var userId = GetUserId();
        var result = await reportInstanceService.UpdateAsync(id, request, userId, ct);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/revert")]
    public async Task<ActionResult<ApiResponse<ReportInstanceResponse>>> Revert(
        Guid id,
        [FromBody] RevertReportInstanceRequest request,
        CancellationToken ct
    )
    {
        var userId = GetUserId();
        var result = await reportInstanceService.RevertAsync(id, request, userId, ct);
        return FromResult(result);
    }
}
