namespace Sicre.Api.Features.Auth.Dtos;

public sealed record LoginResponseDto(
    string Token,
    DateTime Expiration,
    bool RequiresTwoFactor,
    bool HasChangedDefaultPassword
);

public sealed record LoginInternalResponseDto(
    string Token,
    DateTime Expiration,
    bool RequiresTwoFactor,
    bool HasChangedDefaultPassword,
    string? RefreshToken,
    DateTime? RefreshTokenExpiration
);
