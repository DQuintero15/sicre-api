using Sicre.Api.Shared;

namespace Sicre.Api.Features.ControlEntities.Dtos;

public class ControlEntityDto
{
    public Guid Id { get; set; }
    public string? Abbreviation { get; set; }
    public string? Nit { get; set; }
    public required string Name { get; set; }
    public string? LegalBasis { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateControlEntityDto
{
    public string? Abbreviation { get; set; }
    public string? Nit { get; set; }
    public required string Name { get; set; }
    public string? LegalBasis { get; set; }
    public string? Website { get; set; }
}

public class UpdateControlEntityDto
{
    public string? Abbreviation { get; set; }
    public string? Nit { get; set; }
    public required string Name { get; set; }
    public string? LegalBasis { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; }
}

public class ControlEntityFilterDto : PagedRequestDto
{
    public string? Name { get; set; }
    public string? Nit { get; set; }
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}
