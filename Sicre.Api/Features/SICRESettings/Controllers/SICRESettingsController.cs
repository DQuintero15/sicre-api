using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Features.SICRESettings.Dtos;
using Sicre.Api.Features.SICRESettings.Services;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.SICRESettings.Controllers;

[ApiController]
[Route("api/sicre-settings")]
[Authorize(Roles = nameof(Domain.Enums.Role.Administrator))]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class SICRESettingsController(ISICRESettingsService settingsService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<SICRESettingsResponse>>> Get(CancellationToken ct)
    {
        var result = await settingsService.GetAsync(ct);
        return FromResult(result);
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<SICRESettingsResponse>>> Update(
        [FromBody] UpdateSICRESettingsRequest request,
        CancellationToken ct
    )
    {
        var result = await settingsService.UpdateAsync(request, GetUserId(), ct);
        return FromResult(result);
    }
}
