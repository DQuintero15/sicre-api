using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Features.Reports.Dtos.Requests;
using Sicre.Api.Features.Reports.Dtos.Responses;
using Sicre.Api.Features.Reports.Services;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Reports.Controllers;

[ApiController]
[Route("api/reports")]
[Tags("Reports")]
[Authorize]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class ReportsController(IReportService reportService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ReportSummaryResponse>>>> GetAll(
        [FromQuery] GetReportsRequest request,
        CancellationToken ct
    )
    {
        var result = await reportService.GetAllAsync(request, GetUserId(), GetUserRole(), ct);
        return FromResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ReportResponse>>> GetById(
        Guid id,
        CancellationToken ct
    )
    {
        var result = await reportService.GetByIdAsync(id, ct);
        return FromResult(result);
    }

    [HttpPost]
    [Authorize(Roles = nameof(Domain.Enums.Role.Administrator))]
    public async Task<ActionResult<ApiResponse<ReportResponse>>> Create(
        [FromBody] CreateReportRequest request,
        CancellationToken ct
    )
    {
        var userId = GetUserId();
        var result = await reportService.CreateAsync(request, userId, ct);
        if (result.Success)
            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
        return FromResult(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(Domain.Enums.Role.Administrator))]
    public async Task<ActionResult<ApiResponse<ReportResponse>>> Update(
        Guid id,
        [FromBody] UpdateReportRequest request,
        CancellationToken ct
    )
    {
        var userId = GetUserId();
        var result = await reportService.UpdateAsync(id, request, userId, ct);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(Domain.Enums.Role.Administrator))]
    public async Task<ActionResult<ApiResponse<bool>>> Deactivate(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await reportService.DeactivateAsync(id, userId, ct);
        return FromResult(result);
    }

    /// <summary>Importa reportes desde un archivo .json (multipart/form-data, campo "file").</summary>
    [HttpPost("import")]
    [Authorize(Roles = nameof(Domain.Enums.Role.Administrator))]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> ImportFromFile(
        [FromServices] IReportImportService importService,
        IFormFile file,
        CancellationToken ct
    )
    {
        ReportImportRequest? request;
        using (var stream = file.OpenReadStream())
        {
            request = await System.Text.Json.JsonSerializer.DeserializeAsync<ReportImportRequest>(
                stream,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct
            );
        }

        if (request is null)
            return BadRequest(new { result = "error", detail = "Could not parse JSON file." });

        var userId = GetUserId();
        var success = await importService.ImportAsync(request, userId, ct);
        return success ? Ok(new { result = "success" }) : StatusCode(500, new { result = "error" });
    }
}
