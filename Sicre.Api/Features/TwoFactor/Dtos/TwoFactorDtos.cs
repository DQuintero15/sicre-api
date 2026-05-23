namespace Sicre.Api.Features.TwoFactor.Dtos;

public class ValidateTwoFactorCodeRequestDto
{
    public Guid UserId { get; set; }
    public string Secret { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class TwoFactorSetupResponseDto
{
    public string Secret { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
}
