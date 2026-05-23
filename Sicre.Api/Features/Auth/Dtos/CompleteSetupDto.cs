namespace Sicre.Api.Features.Auth.Dtos;

public sealed record CompleteSetupResponseDto(string Token, DateTime Expiration);

public sealed record CompleteSetupInternalDto(
    string Token,
    DateTime Expiration,
    string RefreshToken,
    DateTime RefreshTokenExpiration
);
