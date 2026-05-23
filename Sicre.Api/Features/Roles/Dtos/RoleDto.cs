namespace Sicre.Api.Features.Roles.Dtos;

public class RoleDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? DisplayName { get; set; }
}
