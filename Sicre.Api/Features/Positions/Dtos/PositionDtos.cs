using Sicre.Api.Shared;

namespace Sicre.Api.Features.Positions.Dtos;

public class PositionDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
}

public class CreatePositionDto
{
    public required string Name { get; set; }
}

public class UpdatePositionDto
{
    public required string Name { get; set; }
}

public class PositionFilterDto : PagedRequestDto
{
    public string? Name { get; set; }
}
