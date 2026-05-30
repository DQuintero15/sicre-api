using System.Web;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sicre.Api.Config;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Features.GoogleDrive.Dtos;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.GoogleDrive.Controllers;

[ApiController]
[Route("api/google-drive-auth")]
[Authorize(Roles = nameof(Domain.Enums.Role.Administrator))]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class GoogleDriveController(
    ApplicationDbContext db,
    IOptions<AppSettings> options,
    ILogger<GoogleDriveController> logger
) : BaseController
{
    private GoogleDriveSettings Settings => options.Value.GoogleDrive;
    private string FrontendUrl => options.Value.FrontendUrl;

    /// <summary>Devuelve la URL de autorización de Google OAuth2 para conectar Drive.</summary>
    [HttpGet("url")]
    public ActionResult<ApiResponse<string>> GetAuthUrl()
    {
        if (
            string.IsNullOrWhiteSpace(Settings.ClientId)
            || string.IsNullOrWhiteSpace(Settings.RedirectUri)
        )
            return FromResult(
                ApiResponse<string>.Fail(
                    System.Net.HttpStatusCode.ServiceUnavailable,
                    "Google Drive no está configurado en appsettings."
                )
            );

        var flow = BuildFlow();
        var baseUrl = flow.CreateAuthorizationCodeRequest(Settings.RedirectUri).Build().AbsoluteUri;
        var url = BuildUniqueOAuthUrl(baseUrl);
        return FromResult(ApiResponse<string>.Ok(url));
    }

    /// <summary>Callback OAuth2. Recibe el código de autorización y persiste el token.</summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string? error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            logger.LogWarning("Google Drive OAuth denegado: {Error}", error);
            return Redirect($"{FrontendUrl}?drive=error&reason={Uri.EscapeDataString(error)}");
        }

        if (string.IsNullOrWhiteSpace(code))
            return Redirect($"{FrontendUrl}?drive=error&reason=missing_code");

        try
        {
            var flow = BuildFlow();
            var tokenResponse = await flow.ExchangeCodeForTokenAsync(
                "app",
                code,
                Settings.RedirectUri,
                CancellationToken.None
            );

            var existing = await db.GoogleDriveTokens.FirstOrDefaultAsync();
            var hasIncomingRefreshToken = !string.IsNullOrWhiteSpace(tokenResponse.RefreshToken);
            var hasExistingRefreshToken =
                existing is not null && !string.IsNullOrWhiteSpace(existing.RefreshToken);

            if (!hasIncomingRefreshToken && !hasExistingRefreshToken)
            {
                logger.LogWarning(
                    "Google Drive OAuth callback sin refresh_token y sin token previo válido."
                );
                return Redirect($"{FrontendUrl}?drive=error&reason=requires_reconnect");
            }

            if (existing is null)
            {
                db.GoogleDriveTokens.Add(
                    new GoogleDriveToken
                    {
                        AccessToken = tokenResponse.AccessToken,
                        RefreshToken = tokenResponse.RefreshToken!,
                        ExpiresAt = DateTime.UtcNow.AddSeconds(
                            tokenResponse.ExpiresInSeconds ?? 3600
                        ),
                        UpdatedAt = DateTime.UtcNow,
                    }
                );
            }
            else
            {
                existing.AccessToken = tokenResponse.AccessToken;
                if (!string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
                    existing.RefreshToken = tokenResponse.RefreshToken;
                existing.ExpiresAt = DateTime.UtcNow.AddSeconds(
                    tokenResponse.ExpiresInSeconds ?? 3600
                );
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Google Drive conectado exitosamente.");
            return Redirect($"{FrontendUrl}?drive=connected");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al conectar Google Drive.");
            return Redirect($"{FrontendUrl}?drive=error&reason=unknown");
        }
    }

    /// <summary>Estado de la conexión con Google Drive.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<GoogleDriveStatusDto>>> GetStatus()
    {
        return await GetDriveStatus();
    }

    /// <summary>Alias legacy para mantener compatibilidad con clientes antiguos.</summary>
    [HttpGet("~/api/google-drive/status")]
    public async Task<ActionResult<ApiResponse<GoogleDriveStatusDto>>> GetLegacyStatus()
    {
        return await GetDriveStatus();
    }

    private async Task<ActionResult<ApiResponse<GoogleDriveStatusDto>>> GetDriveStatus()
    {
        var token = await db.GoogleDriveTokens.FirstOrDefaultAsync();

        var hasRefreshToken = token is not null && !string.IsNullOrWhiteSpace(token.RefreshToken);
        var requiresReconnect = token is not null && !hasRefreshToken;
        var isConnected = hasRefreshToken;
        var isExpired = token is not null && token.ExpiresAt <= DateTime.UtcNow;

        return FromResult(
            ApiResponse<GoogleDriveStatusDto>.Ok(
                new GoogleDriveStatusDto
                {
                    IsConnected = isConnected,
                    RequiresReconnect = requiresReconnect,
                    IsTokenExpired = isConnected && isExpired,
                    TokenExpiresAt = token?.ExpiresAt,
                    UpdatedAt = token?.UpdatedAt,
                }
            )
        );
    }

    /// <summary>Desconecta la cuenta de Google Drive eliminando el token almacenado.</summary>
    [HttpPost("disable")]
    public async Task<ActionResult<ApiResponse<bool>>> Disable()
    {
        var token = await db.GoogleDriveTokens.FirstOrDefaultAsync();
        if (token is null)
            return FromResult(
                ApiResponse<bool>.Fail(
                    System.Net.HttpStatusCode.NotFound,
                    "No hay cuenta de Google Drive conectada."
                )
            );

        db.GoogleDriveTokens.Remove(token);
        await db.SaveChangesAsync();
        logger.LogInformation("Google Drive desconectado.");
        return FromResult(ApiResponse<bool>.Ok(true, "Google Drive desconectado exitosamente."));
    }

    private GoogleAuthorizationCodeFlow BuildFlow() =>
        new(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new Google.Apis.Auth.OAuth2.ClientSecrets
                {
                    ClientId = Settings.ClientId,
                    ClientSecret = Settings.ClientSecret,
                },
                Scopes = [DriveService.ScopeConstants.Drive],
            }
        );

    private static string BuildUniqueOAuthUrl(string baseUrl)
    {
        var uriBuilder = new UriBuilder(baseUrl);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        query.Set("access_type", "offline");
        query.Set("prompt", "consent");
        query.Set("include_granted_scopes", "true");

        uriBuilder.Query = query.ToString() ?? string.Empty;
        return uriBuilder.Uri.AbsoluteUri;
    }
}
