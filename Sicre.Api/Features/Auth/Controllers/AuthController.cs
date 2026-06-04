using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Features.Auth.Dtos;
using Sicre.Api.Features.Auth.Services;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    ICookieService cookieService,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService
) : BaseController
{
    [HttpGet("needs-password-setup")]
    [Authorize]
    [RequireTokenType(Constants.TokenTypes.Temporary, Constants.TokenTypes.AccessToken)]
    public async Task<ActionResult<ApiResponse<bool>>> NeedsPasswordSetup()
    {
        var result = await authService.CheckIfUserChangedPasswordAsync(GetUserId());
        return FromResult(result);
    }

    [HttpPost("complete-setup")]
    [Authorize]
    [RequireTokenType(Constants.TokenTypes.Temporary, Constants.TokenTypes.AccessToken)]
    public async Task<ActionResult<ApiResponse<CompleteSetupResponseDto>>> CompleteSetup(
        [FromBody] ChangePasswordRequestDto request
    )
    {
        var result = await authService.CompleteSetupAsync(GetUserId(), request);

        if (result.Success)
            cookieService.SetRefreshTokenCookie(
                Response,
                Request,
                result.Data!.RefreshToken,
                result.Data.RefreshTokenExpiration
            );

        var pub = result.Success
            ? ApiResponse<CompleteSetupResponseDto>.Ok(
                new CompleteSetupResponseDto(result.Data!.Token, result.Data.Expiration),
                result.Message
            )
            : ApiResponse<CompleteSetupResponseDto>.Fail(
                result.StatusCode,
                result.Message ?? "Error"
            );

        return FromResult(pub);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginRequestDto request
    )
    {
        var response = await authService.LoginAsync(request);

        if (response.Success && response.Data?.RefreshToken != null)
            cookieService.SetRefreshTokenCookie(
                Response,
                Request,
                response.Data.RefreshToken,
                response.Data.RefreshTokenExpiration!.Value
            );

        var pub = response.Success
            ? ApiResponse<LoginResponseDto>.Ok(
                new LoginResponseDto(
                    response.Data!.Token,
                    response.Data.Expiration,
                    response.Data.RequiresTwoFactor,
                    response.Data.HasChangedDefaultPassword
                ),
                response.Message
            )
            : ApiResponse<LoginResponseDto>.Fail(response.StatusCode, response.Message ?? "Error");

        return FromResult(pub);
    }

    [HttpPost("login/verify-2fa")]
    [Authorize]
    [RequireTokenType(Constants.TokenTypes.Temporary)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> VerifyTwoFactorLogin(
        [FromBody] VerifyTwoFactorLoginRequestDto request
    )
    {
        var response = await authService.VerifyTwoFactorLoginAsync(GetUserId(), request.Code);

        if (response.Success && response.Data?.RefreshToken != null)
            cookieService.SetRefreshTokenCookie(
                Response,
                Request,
                response.Data.RefreshToken,
                response.Data.RefreshTokenExpiration!.Value
            );

        var pub = response.Success
            ? ApiResponse<LoginResponseDto>.Ok(
                new LoginResponseDto(
                    response.Data!.Token,
                    response.Data.Expiration,
                    response.Data.RequiresTwoFactor,
                    response.Data.HasChangedDefaultPassword
                ),
                response.Message
            )
            : ApiResponse<LoginResponseDto>.Fail(response.StatusCode, response.Message ?? "Error");

        return FromResult(pub);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<RefreshTokenResponseDto>>> RefreshToken()
    {
        if (
            !cookieService.TryGetRefreshTokenCookie(Request, out var refreshToken)
            || string.IsNullOrEmpty(refreshToken)
        )
            return FromResult(
                ApiResponse<RefreshTokenResponseDto>.Fail(
                    System.Net.HttpStatusCode.Unauthorized,
                    "Refresh token not found in cookies."
                )
            );

        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return FromResult(
                ApiResponse<RefreshTokenResponseDto>.Fail(
                    System.Net.HttpStatusCode.Unauthorized,
                    "Missing or invalid Authorization header."
                )
            );

        var token = authHeader["Bearer ".Length..];
        var principal = tokenService.ValidateTokenIgnoreExpiration(token);

        if (principal == null)
            return FromResult(
                ApiResponse<RefreshTokenResponseDto>.Fail(
                    System.Net.HttpStatusCode.Unauthorized,
                    "Invalid token."
                )
            );

        var userIdValue = principal
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?.Value;
        if (string.IsNullOrEmpty(userIdValue) || !Guid.TryParse(userIdValue, out var userIdGuid))
            return FromResult(
                ApiResponse<RefreshTokenResponseDto>.Fail(
                    System.Net.HttpStatusCode.Unauthorized,
                    "Invalid or missing user information in token."
                )
            );

        var response = await authService.RefreshTokenAsync(userIdGuid, refreshToken);

        if (response.Success)
            cookieService.SetRefreshTokenCookie(
                Response,
                Request,
                response.Data!.RefreshToken,
                response.Data.RefreshTokenExpiration
            );

        var pub = response.Success
            ? ApiResponse<RefreshTokenResponseDto>.Ok(
                new RefreshTokenResponseDto(response.Data!.Token, response.Data.Expiration),
                response.Message
            )
            : ApiResponse<RefreshTokenResponseDto>.Fail(
                response.StatusCode,
                response.Message ?? "Error"
            );

        return FromResult(pub);
    }

    [HttpPost("two-factor-is-enable")]
    [Authorize]
    [RequireTokenType(Constants.TokenTypes.AccessToken)]
    public async Task<ActionResult<ApiResponse<bool>>> GetTwoFactorEnabled()
    {
        var result = await authService.GetTwoFactorEnabledAsync(GetUserId());
        return FromResult(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> Logout()
    {
        await refreshTokenService.RevokeRefreshTokenAsync(GetUserId());
        cookieService.RemoveRefreshTokenCookie(Response);
        return FromResult(ApiResponse<bool>.Ok(true, "Sesión cerrada exitosamente."));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request
    )
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var result = await authService.ForgotPasswordAsync(request, ip, userAgent);
        return FromResult(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(
        [FromBody] ResetPasswordRequestDto request
    )
    {
        var result = await authService.ResetPasswordAsync(request);
        return FromResult(result);
    }
}
