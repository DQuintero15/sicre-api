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
[Route("api/google-drive")]
[Authorize(Roles = nameof(Domain.Enums.Role.Administrator))]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class GoogleDriveController(
    ApplicationDbContext db,
    IOptions<AppSettings> options,
    ILogger<GoogleDriveController> logger
) : BaseController
{
    private GoogleDriveSettings Settings => options.Value.GoogleDrive;

    /// <summary>Devuelve la URL de autorización de Google OAuth2 para conectar Drive.</summary>
    [HttpGet("auth-url")]
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
        var url = flow.CreateAuthorizationCodeRequest(Settings.RedirectUri).Build().AbsoluteUri;
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
            return BadRequest(new { message = $"Autorización denegada: {error}" });
        }

        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { message = "Código de autorización faltante." });

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

            if (existing is null)
            {
                db.GoogleDriveTokens.Add(
                    new GoogleDriveToken
                    {
                        AccessToken = tokenResponse.AccessToken,
                        RefreshToken = tokenResponse.RefreshToken ?? string.Empty,
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
                if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                    existing.RefreshToken = tokenResponse.RefreshToken;
                existing.ExpiresAt = DateTime.UtcNow.AddSeconds(
                    tokenResponse.ExpiresInSeconds ?? 3600
                );
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Google Drive conectado exitosamente.");
            return Ok(new { message = "Google Drive conectado exitosamente." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al conectar Google Drive.");
            return StatusCode(
                500,
                new { message = "Error al procesar la autorización de Google Drive." }
            );
        }
    }

    /// <summary>Estado de la conexión con Google Drive.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<GoogleDriveStatusDto>>> GetStatus()
    {
        var token = await db.GoogleDriveTokens.FirstOrDefaultAsync();
        return FromResult(
            ApiResponse<GoogleDriveStatusDto>.Ok(
                new GoogleDriveStatusDto
                {
                    IsConnected = token is not null,
                    TokenExpiresAt = token?.ExpiresAt,
                    UpdatedAt = token?.UpdatedAt,
                }
            )
        );
    }

    /// <summary>Desconecta la cuenta de Google Drive eliminando el token almacenado.</summary>
    [HttpDelete("disconnect")]
    public async Task<ActionResult<ApiResponse<bool>>> Disconnect()
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
}
