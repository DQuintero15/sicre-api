namespace Sicre.Api.Features.Auth.Dtos;

public sealed record ChangePasswordRequestDto(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
);
