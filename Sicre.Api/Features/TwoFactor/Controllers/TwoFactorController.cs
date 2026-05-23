using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Features.TwoFactor.Dtos;
using Sicre.Api.Features.TwoFactor.Services;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.TwoFactor.Controllers;

[ApiController]
[Route("api/twofactor")]
public class TwoFactorController(ITwoFactorService twoFactorService) : BaseController
{
    [HttpPost("setup")]
    [Authorize]
    [RequireTokenType(Constants.TokenTypes.Temporary, Constants.TokenTypes.AccessToken)]
    public async Task<ActionResult<ApiResponse<TwoFactorSetupResponseDto>>> Setup()
    {
        var result = await twoFactorService.GenerateMfaSetup(GetUserId());
        return FromResult(result);
    }

    [HttpPost("validate")]
    [Authorize]
    [RequireTokenType(Constants.TokenTypes.Temporary, Constants.TokenTypes.AccessToken)]
    public async Task<ActionResult<ApiResponse<bool>>> Validate(
        [FromBody] ValidateTwoFactorCodeRequestDto dto
    )
    {
        dto.UserId = GetUserId();
        var result = await twoFactorService.ValidateCode(dto);
        return FromResult(result);
    }
}
