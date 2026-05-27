namespace Sicre.Api.Features.Users.Dtos;

public sealed record ProfileDto(
    Guid Id,
    string FullName,
    IList<string> Roles,
    string? Email,
    string? PositionName,
    string? ProcessName,
    string? BranchName
);
