namespace Sicre.Api.Domain.Entities;

public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public required string Action { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public Guid PerformedByUserId { get; set; }
    public User? PerformedByUser { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string? MetadataJson { get; set; }
}
