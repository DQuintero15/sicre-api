using Sicre.Api.Domain.Enums;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Users.Dtos;

public class UserDto
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public Guid? PositionId { get; set; }
    public string? PositionName { get; set; }
    public Guid? ProcessId { get; set; }
    public string? ProcessName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? RoleName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool HasChangedDefaultPassword { get; set; }
}

public class CreateUserDto
{
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? ProcessId { get; set; }
    public Guid? BranchId { get; set; }
    public required string Role { get; set; }
}

public class UpdateUserDto
{
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public required string Role { get; set; }
    public Guid? PositionId { get; set; }
    public Guid? ProcessId { get; set; }
    public Guid? BranchId { get; set; }
    public bool IsActive { get; set; }
}

public class UserFilterDto : PagedRequestDto
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool? IsActive { get; set; }
    public Role? Role { get; set; }
    public string? Search { get; set; }
}

public class ImportUsersFromCsvResponseDto
{
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int CreatedUsers { get; set; }
    public int UpdatedUsers { get; set; }
    public int FailedRows { get; set; }
    public List<ImportUsersFromCsvRowResultDto> Rows { get; set; } = [];
}

public class ResendTemporaryPasswordBulkResponseDto
{
    public int TotalPending { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
}

public class ImportUsersFromCsvRowResultDto
{
    public int RowNumber { get; set; }
    public string? Email { get; set; }
    public bool Success { get; set; }
    public bool Created { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RoleAdded { get; set; }
}
