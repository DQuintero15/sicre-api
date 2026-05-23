namespace Sicre.Api.Features.Auth.Dtos;

public sealed record ResetPasswordRequestDto(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword
);
