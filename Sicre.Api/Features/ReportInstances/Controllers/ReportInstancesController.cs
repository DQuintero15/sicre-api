using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Domain.Enums;
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
public class ReportInstancesController(
    IReportInstanceService reportInstanceService,
    IReportAttachmentService attachmentService
) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ReportInstanceSummaryResponse>>>> GetAll(
        [FromQuery] GetReportInstancesRequest request,
        CancellationToken ct
    )
    {
        var result = await reportInstanceService.GetAllAsync(
            request,
            GetUserId(),
            GetUserRole(),
            ct
        );
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

    [HttpPost("{id:guid}/deliver")]
    public async Task<ActionResult<ApiResponse<ReportInstanceResponse>>> Deliver(
        Guid id,
        [FromBody] DeliverRequest request,
        CancellationToken ct
    )
    {
        var userId = GetUserId();
        var result = await reportInstanceService.DeliverAsync(id, request, userId, ct);
        return FromResult(result);
    }

    // ── Attachments ──────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/attachments")]
    public async Task<
        ActionResult<ApiResponse<PagedResult<ReportAttachmentResponse>>>
    > GetAttachments(Guid id, CancellationToken ct)
    {
        var result = await attachmentService.GetByInstanceAsync(id, ct);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(31_457_280)] // 30 MB
    public async Task<ActionResult<ApiResponse<ReportAttachmentResponse>>> AddFileAttachment(
        Guid id,
        [FromForm] AddFileAttachmentRequest request,
        IFormFile file,
        CancellationToken ct
    )
    {
        if (file is null || file.Length == 0)
            return BadRequest(
                ApiResponse<ReportAttachmentResponse>.Fail(
                    System.Net.HttpStatusCode.BadRequest,
                    "No se proporcionó ningún archivo."
                )
            );

        if (file.Length > 30 * 1024 * 1024)
            return BadRequest(
                ApiResponse<ReportAttachmentResponse>.Fail(
                    System.Net.HttpStatusCode.BadRequest,
                    "El archivo supera el tamaño máximo de 30 MB."
                )
            );

        var userId = GetUserId();
        await using var stream = file.OpenReadStream();
        var result = await attachmentService.AddFileAsync(
            id,
            request.Type,
            stream,
            file.FileName,
            file.ContentType,
            userId
        );
        return FromResult(result);
    }

    [HttpGet("{id:guid}/attachments/{uploadId:guid}/progress")]
    [AllowAnonymous]
    public async Task GetAttachmentProgress(Guid id, Guid uploadId)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");

        while (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            var progress = await attachmentService.GetProgressAsync(uploadId);

            if (progress is not null)
            {
                var payload = JsonSerializer.Serialize(progress.ToString());
                await Response.WriteAsync($"data: {payload}\n\n");
                await Response.Body.FlushAsync();

                if (progress == UploadProgress.Completed || progress == UploadProgress.Failed)
                {
                    break;
                }
            }

            try
            {
                await Task.Delay(1000, HttpContext.RequestAborted);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    [HttpPost("{id:guid}/attachments/reversion")]
    [Authorize(Roles = nameof(Domain.Enums.Role.Administrator))]
    [RequestSizeLimit(31_457_280)]
    public async Task<ActionResult<ApiResponse<ReportAttachmentResponse>>> AddReversionAttachment(
        Guid id,
        IFormFile file,
        [FromForm] string? notes,
        CancellationToken ct
    )
    {
        if (file is null || file.Length == 0)
            return BadRequest(
                ApiResponse<ReportAttachmentResponse>.Fail(
                    System.Net.HttpStatusCode.BadRequest,
                    "No se proporcionó ningún archivo."
                )
            );

        if (file.Length > 30 * 1024 * 1024)
            return BadRequest(
                ApiResponse<ReportAttachmentResponse>.Fail(
                    System.Net.HttpStatusCode.BadRequest,
                    "El archivo supera el tamaño máximo de 30 MB."
                )
            );

        var userId = GetUserId();
        await using var stream = file.OpenReadStream();
        var result = await attachmentService.AddReversionFileAsync(
            id,
            stream,
            file.FileName,
            file.ContentType,
            userId,
            notes
        );
        return FromResult(result);
    }
}
