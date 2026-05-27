using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sicre.Api.Config;
using Sicre.Api.Features.Audit.DTOs;
using Sicre.Api.Features.Audit.Services;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Audit.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Administrator,Auditor")]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class AuditController(
    IAuditQueryService auditQueryService,
    IOptions<AppSettings> options
) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditEventResponse>>>> GetAll(
        [FromQuery] GetAuditRequest request,
        CancellationToken ct
    )
    {
        if (!options.Value.Features.GlobalAudit)
            return NotFound();

        var result = await auditQueryService.GetAllAsync(request, ct);
        return FromResult(result);
    }
}
