using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sicre.Api.Config;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Features.Auth.Dtos;
using Sicre.Api.Features.TwoFactor.Dtos;
using Sicre.Api.Features.TwoFactor.Services;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;
using Sicre.Api.Shared.Email;

namespace Sicre.Api.Features.Auth.Services;

public interface IAuthService
{
    Task<ApiResponse<LoginInternalResponseDto>> LoginAsync(LoginRequestDto dto);
    Task<ApiResponse<bool>> CheckIfUserChangedPasswordAsync(Guid userId);
    Task<ApiResponse<CompleteSetupInternalDto>> CompleteSetupAsync(
        Guid userId,
        ChangePasswordRequestDto dto
    );
    Task<ApiResponse<RefreshTokenInternalDto>> RefreshTokenAsync(Guid userId, string refreshToken);
    Task<ApiResponse<bool>> GetTwoFactorEnabledAsync(Guid userId);
    Task<ApiResponse<LoginInternalResponseDto>> VerifyTwoFactorLoginAsync(Guid userId, string code);
    Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequestDto dto, string? ipAddress, string? userAgent);
    Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequestDto dto);
}

public class AuthService(
    ILogger<AuthService> logger,
    ITokenService tokenService,
    IOptions<AppSettings> options,
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IRefreshTokenService refreshTokenService,
    ITwoFactorService twoFactorService,
    ApplicationDbContext db,
    IEmailService emailService,
    IEmailTemplateService emailTemplateService
) : IAuthService
{
    private readonly AppSettings _settings = options.Value;
    private const int MaxPasswordResetRequestsPerHour = 3;

    public async Task<ApiResponse<LoginInternalResponseDto>> LoginAsync(LoginRequestDto dto)
    {
        try
        {
            var user = await userManager.FindByEmailAsync(dto.Email);

            if (user == null)
                return ApiResponse<LoginInternalResponseDto>.Fail(
                    HttpStatusCode.Unauthorized,
                    "Correo electrónico o contraseña incorrectos."
                );

            var result = await signInManager.CheckPasswordSignInAsync(
                user,
                dto.Password,
                lockoutOnFailure: false
            );

            if (!result.Succeeded)
                return ApiResponse<LoginInternalResponseDto>.Fail(
                    HttpStatusCode.Unauthorized,
                    "Correo electrónico o contraseña incorrectos."
                );

            var requiresTwoFactor = await userManager.GetTwoFactorEnabledAsync(user);

            if (!requiresTwoFactor)
            {
                var roles = await userManager.GetRolesAsync(user);
                var accessToken = tokenService.GenerateAccessToken(user, roles);
                var (refreshToken, refreshExpires) = tokenService.GenerateRefreshToken();

                var saved = await refreshTokenService.CreateAndSaveRefreshTokenAsync(
                    user.Id,
                    refreshToken,
                    refreshExpires
                );
                if (!saved.Success)
                    return ApiResponse<LoginInternalResponseDto>.Fail(
                        HttpStatusCode.InternalServerError,
                        "An error occurred while processing the token."
                    );

                return ApiResponse<LoginInternalResponseDto>.Ok(
                    new LoginInternalResponseDto(
                        accessToken,
                        DateTime.UtcNow.AddMinutes(_settings.Jwt.AccessTokenExpirationMinutes),
                        requiresTwoFactor,
                        user.HasChangedDefaultPassword,
                        refreshToken,
                        refreshExpires
                    ),
                    "Inicio de sesión exitoso."
                );
            }

            var tempToken = tokenService.GenerateTemporaryToken(user);

            return ApiResponse<LoginInternalResponseDto>.Ok(
                new LoginInternalResponseDto(
                    tempToken,
                    DateTime.UtcNow.AddMinutes(_settings.Jwt.TemporaryTokenExpirationMinutes),
                    requiresTwoFactor,
                    user.HasChangedDefaultPassword,
                    null,
                    null
                ),
                "Inicio de sesión exitoso."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante login");
            return ApiResponse<LoginInternalResponseDto>.Fail(
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    public async Task<ApiResponse<bool>> CheckIfUserChangedPasswordAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return ApiResponse<bool>.Fail(HttpStatusCode.NotFound, "Usuario no encontrado.");

        return ApiResponse<bool>.Ok(
            user.HasChangedDefaultPassword,
            "Estado de cambio de contraseña recuperado exitosamente."
        );
    }

    public async Task<ApiResponse<CompleteSetupInternalDto>> CompleteSetupAsync(
        Guid userId,
        ChangePasswordRequestDto dto
    )
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return ApiResponse<CompleteSetupInternalDto>.Fail(
                    HttpStatusCode.NotFound,
                    "Usuario no encontrado."
                );

            if (user.HasChangedDefaultPassword)
                return ApiResponse<CompleteSetupInternalDto>.Fail(
                    HttpStatusCode.BadRequest,
                    "La contraseña ya ha sido cambiada previamente."
                );

            var result = await userManager.ChangePasswordAsync(
                user,
                dto.CurrentPassword,
                dto.NewPassword
            );

            if (!result.Succeeded)
                return ApiResponse<CompleteSetupInternalDto>.Fail(
                    HttpStatusCode.BadRequest,
                    "No se pudo cambiar la contraseña. Verifique las credenciales e intente nuevamente."
                );

            user.HasChangedDefaultPassword = true;
            await userManager.UpdateAsync(user);

            var roles = await userManager.GetRolesAsync(user);
            var accessToken = tokenService.GenerateAccessToken(user, roles);
            var (refreshToken, refreshExpires) = tokenService.GenerateRefreshToken();

            var saved = await refreshTokenService.CreateAndSaveRefreshTokenAsync(
                userId,
                refreshToken,
                refreshExpires
            );
            if (!saved.Success)
                return ApiResponse<CompleteSetupInternalDto>.Fail(
                    HttpStatusCode.InternalServerError,
                    "An error occurred while processing the token."
                );

            return ApiResponse<CompleteSetupInternalDto>.Ok(
                new CompleteSetupInternalDto(
                    accessToken,
                    DateTime.UtcNow.AddMinutes(_settings.Jwt.AccessTokenExpirationMinutes),
                    refreshToken,
                    refreshExpires
                ),
                "Contraseña cambiada exitosamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al cambiar contraseña para usuario {UserId}", userId);
            return ApiResponse<CompleteSetupInternalDto>.Fail(
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    public async Task<ApiResponse<RefreshTokenInternalDto>> RefreshTokenAsync(
        Guid userId,
        string refreshToken
    )
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return ApiResponse<RefreshTokenInternalDto>.Fail(
                    HttpStatusCode.NotFound,
                    "Usuario no encontrado."
                );

            var validation = await refreshTokenService.ValidateRefreshTokenAsync(
                userId,
                refreshToken
            );
            if (!validation.Success)
                return ApiResponse<RefreshTokenInternalDto>.Fail(
                    validation.StatusCode,
                    validation.Message ?? "Token inválido o expirado."
                );

            var roles = await userManager.GetRolesAsync(user);
            var newAccess = tokenService.GenerateAccessToken(user, roles);
            var (newRefresh, newExpires) = tokenService.GenerateRefreshToken();

            var saved = await refreshTokenService.CreateAndSaveRefreshTokenAsync(
                userId,
                newRefresh,
                newExpires
            );
            if (!saved.Success)
                return ApiResponse<RefreshTokenInternalDto>.Fail(
                    HttpStatusCode.InternalServerError,
                    "An error occurred while processing the token."
                );

            return ApiResponse<RefreshTokenInternalDto>.Ok(
                new RefreshTokenInternalDto(
                    newAccess,
                    DateTime.UtcNow.AddMinutes(_settings.Jwt.AccessTokenExpirationMinutes),
                    newRefresh,
                    newExpires
                ),
                "Token actualizado exitosamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al refrescar token para usuario {UserId}", userId);
            return ApiResponse<RefreshTokenInternalDto>.Fail(
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    public async Task<ApiResponse<bool>> GetTwoFactorEnabledAsync(Guid userId)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ApiResponse<bool>.Fail(HttpStatusCode.Forbidden, "Usuario no encontrado.");

            return ApiResponse<bool>.Ok(
                await userManager.GetTwoFactorEnabledAsync(user),
                "2FA status retrieved successfully."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener estado 2FA para usuario {UserId}", userId);
            return ApiResponse<bool>.Fail(
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please try again later."
            );
        }
    }

    public async Task<ApiResponse<LoginInternalResponseDto>> VerifyTwoFactorLoginAsync(
        Guid userId,
        string code
    )
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return ApiResponse<LoginInternalResponseDto>.Fail(
                    HttpStatusCode.BadRequest,
                    "Usuario no encontrado."
                );

            if (string.IsNullOrEmpty(user.TwoFactorSecret))
                return ApiResponse<LoginInternalResponseDto>.Fail(
                    HttpStatusCode.BadRequest,
                    "2FA no está configurado para esta cuenta."
                );

            var validate = await twoFactorService.ValidateCode(
                new ValidateTwoFactorCodeRequestDto
                {
                    UserId = userId,
                    Code = code,
                    Secret = string.Empty,
                }
            );

            if (!validate.Success)
                return ApiResponse<LoginInternalResponseDto>.Fail(
                    HttpStatusCode.BadRequest,
                    validate.Message ?? "Código inválido."
                );

            if (!user.TwoFactorEnabled)
                await userManager.SetTwoFactorEnabledAsync(user, true);

            var roles = await userManager.GetRolesAsync(user);
            var accessToken = tokenService.GenerateAccessToken(user, roles);
            var (refreshToken, refreshExpires) = tokenService.GenerateRefreshToken();

            var saved = await refreshTokenService.CreateAndSaveRefreshTokenAsync(
                userId,
                refreshToken,
                refreshExpires
            );
            if (!saved.Success)
                return ApiResponse<LoginInternalResponseDto>.Fail(
                    HttpStatusCode.InternalServerError,
                    "An error occurred while processing the token."
                );

            return ApiResponse<LoginInternalResponseDto>.Ok(
                new LoginInternalResponseDto(
                    accessToken,
                    DateTime.UtcNow.AddMinutes(_settings.Jwt.AccessTokenExpirationMinutes),
                    false,
                    user.HasChangedDefaultPassword,
                    refreshToken,
                    refreshExpires
                ),
                "2FA validado correctamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al verificar 2FA para usuario {UserId}", userId);
            return ApiResponse<LoginInternalResponseDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al validar el código 2FA. Intente de nuevo."
            );
        }
    }

    public async Task<ApiResponse<bool>> ForgotPasswordAsync(
        ForgotPasswordRequestDto dto,
        string? ipAddress,
        string? userAgent
    )
    {
        try
        {
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var recent = await db.PasswordResetRequests.CountAsync(r =>
                r.Email.ToLower() == dto.Email.ToLower() && r.RequestedAt >= oneHourAgo
            );

            if (recent >= MaxPasswordResetRequestsPerHour)
                return ApiResponse<bool>.Fail(
                    HttpStatusCode.TooManyRequests,
                    "Ha excedido el límite de solicitudes. Intente nuevamente en una hora."
                );

            const string neutral =
                "Si el correo existe, recibirá un enlace para restablecer su contraseña.";
            var user = await userManager.FindByEmailAsync(dto.Email);

            // Always log the request (for audit), regardless of user existence
            var token = user is not null
                ? await userManager.GeneratePasswordResetTokenAsync(user)
                : null;

            var tokenHash = token is not null
                ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))
                : null;

            db.PasswordResetRequests.Add(
                new PasswordResetRequest
                {
                    Id = Guid.NewGuid(),
                    Email = dto.Email.ToLower(),
                    TokenHash = tokenHash ?? string.Empty,
                    RequestedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                }
            );
            await db.SaveChangesAsync();

            if (user == null || !user.IsActive)
                return ApiResponse<bool>.Ok(true, neutral);

            var encodedToken = HttpUtility.UrlEncode(token!);
            var encodedEmail = HttpUtility.UrlEncode(dto.Email);
            var resetLink =
                $"{_settings.FrontendUrl}/auth/reset-password?token={encodedToken}&email={encodedEmail}";

            var fullName = $"{user.FirstName} {user.LastName}";
            var emailBody = emailTemplateService.GetPasswordResetEmailTemplate(fullName, resetLink);
            await emailService.SendEmailAsync(dto.Email, "Restablecer Contrasena - SICRE", emailBody);

            logger.LogInformation(
                "Enlace de restablecimiento enviado a {Email}",
                dto.Email
            );

            return ApiResponse<bool>.Ok(true, neutral);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante forgot-password");
            return ApiResponse<bool>.Fail(
                HttpStatusCode.InternalServerError,
                "Ocurrió un error inesperado. Intente nuevamente más tarde."
            );
        }
    }

    public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequestDto dto)
    {
        try
        {
            var user = await userManager.FindByEmailAsync(dto.Email);

            if (user == null || !user.IsActive)
                return ApiResponse<bool>.Fail(
                    HttpStatusCode.BadRequest,
                    "El enlace de restablecimiento es inválido o ha expirado."
                );

            var result = await userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == "InvalidToken"))
                    return ApiResponse<bool>.Fail(
                        HttpStatusCode.BadRequest,
                        "El enlace de restablecimiento es inválido o ha expirado."
                    );

                return ApiResponse<bool>.Fail(
                    HttpStatusCode.BadRequest,
                    "No se pudo restablecer la contraseña. Verifique que cumpla con los requisitos."
                );
            }

            if (!user.HasChangedDefaultPassword)
            {
                user.HasChangedDefaultPassword = true;
                await userManager.UpdateAsync(user);
            }

            // Mark the reset request as used (anti-reuse)
            var resetRequest = await db.PasswordResetRequests
                .Where(r => r.Email == dto.Email.ToLower() && r.UsedAt == null)
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefaultAsync();
            if (resetRequest is not null)
            {
                resetRequest.UsedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            return ApiResponse<bool>.Ok(
                true,
                "Contraseña restablecida exitosamente. Ya puede iniciar sesión con su nueva contraseña."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante reset-password para {Email}", dto.Email);
            return ApiResponse<bool>.Fail(
                HttpStatusCode.InternalServerError,
                "Ocurrió un error inesperado. Intente nuevamente más tarde."
            );
        }
    }
}
