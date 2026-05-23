using Sicre.Api.Shared;

namespace Sicre.Api.Features.Branches.Dtos;

public class BranchDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; }
}

public class CreateBranchDto
{
    public required string Name { get; set; }
}

public class UpdateBranchDto
{
    public required string Name { get; set; }
    public bool IsActive { get; set; }
}

public class BranchFilterDto : PagedRequestDto
{
    public string? Name { get; set; }
}
