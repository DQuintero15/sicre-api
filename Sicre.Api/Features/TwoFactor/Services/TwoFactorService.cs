using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using OtpNet;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Features.TwoFactor.Dtos;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.TwoFactor.Services;

public interface ITwoFactorService
{
    Task<ApiResponse<TwoFactorSetupResponseDto>> GenerateMfaSetup(Guid userId);
    Task<ApiResponse<bool>> ValidateCode(ValidateTwoFactorCodeRequestDto dto);
}

public class TwoFactorService(
    UserManager<User> userManager,
    IDataProtectionProvider dataProtectionProvider
) : ITwoFactorService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        "TwoFactorSecrets"
    );
    private const string Issuer = "SICRE";

    public async Task<ApiResponse<TwoFactorSetupResponseDto>> GenerateMfaSetup(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            return ApiResponse<TwoFactorSetupResponseDto>.Fail(
                HttpStatusCode.Forbidden,
                "Usuario no encontrado."
            );

        var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
        user.TwoFactorSecret = _protector.Protect(secret);
        await userManager.UpdateAsync(user);

        var emailEscaped = System.Net.WebUtility.UrlEncode(user.Email);
        var issuerEscaped = System.Net.WebUtility.UrlEncode(Issuer);
        var qrCodeUrl =
            $"otpauth://totp/{issuerEscaped}:{emailEscaped}?secret={secret}&issuer={issuerEscaped}&digits=6&period=30";

        return ApiResponse<TwoFactorSetupResponseDto>.Ok(
            new TwoFactorSetupResponseDto { Secret = secret, QrCodeUrl = qrCodeUrl }
        );
    }

    public async Task<ApiResponse<bool>> ValidateCode(ValidateTwoFactorCodeRequestDto dto)
    {
        try
        {
            var secret = dto.Secret;
            User? user = null;

            if (dto.UserId != Guid.Empty)
            {
                user = await userManager.FindByIdAsync(dto.UserId.ToString());
                if (user != null && !string.IsNullOrEmpty(user.TwoFactorSecret))
                {
                    try
                    {
                        secret = _protector.Unprotect(user.TwoFactorSecret);
                    }
                    catch
                    {
                        return ApiResponse<bool>.Fail(
                            HttpStatusCode.BadRequest,
                            "Error al validar el código. Verifique el secreto."
                        );
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(secret))
                return ApiResponse<bool>.Fail(
                    HttpStatusCode.BadRequest,
                    "2FA no está configurado para este usuario."
                );

            var totp = new Totp(Base32Encoding.ToBytes(secret));
            if (!totp.VerifyTotp(dto.Code, out _, new VerificationWindow(1, 1)))
                return ApiResponse<bool>.Fail(HttpStatusCode.BadRequest, "Código inválido.");

            if (user != null && !user.TwoFactorEnabled)
            {
                var enableResult = await userManager.SetTwoFactorEnabledAsync(user, true);
                if (!enableResult.Succeeded)
                    return ApiResponse<bool>.Fail(
                        HttpStatusCode.InternalServerError,
                        "No se pudo habilitar el 2FA para este usuario."
                    );
            }

            return ApiResponse<bool>.Ok(true, "Código validado correctamente.");
        }
        catch
        {
            return ApiResponse<bool>.Fail(
                HttpStatusCode.BadRequest,
                "Error al validar el código. Verifique el secreto."
            );
        }
    }
}
