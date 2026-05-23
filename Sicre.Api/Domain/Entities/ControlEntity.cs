namespace Sicre.Api.Domain.Entities;

public class ControlEntity
{
    public Guid Id { get; set; }
    public string? Abbreviation { get; set; }
    public string? Nit { get; set; }
    public required string Name { get; set; }
    public string? LegalBasis { get; set; }
    public string? Website { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Report> Reports { get; set; } = [];
}
