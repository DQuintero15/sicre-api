namespace Sicre.Api.Domain.Entities;

public class Branch
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Report> Reports { get; set; } = [];
}
