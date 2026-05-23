namespace Sicre.Api.Features.Auth.Dtos;

public sealed record RefreshTokenResponseDto(string Token, DateTime Expiration);

public sealed record RefreshTokenInternalDto(
    string Token,
    DateTime Expiration,
    string RefreshToken,
    DateTime RefreshTokenExpiration
);
