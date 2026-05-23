using Sicre.Api.Shared;

namespace Sicre.Api.Features.Processes.Dtos;

public class ProcessDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
}

public class CreateProcessDto
{
    public required string Name { get; set; }
}

public class UpdateProcessDto
{
    public required string Name { get; set; }
}

public class ProcessFilterDto : PagedRequestDto
{
    public string? Name { get; set; }
}
