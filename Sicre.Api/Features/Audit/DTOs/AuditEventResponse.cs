namespace Sicre.Api.Features.Audit.DTOs;

public class AuditEventResponse
{
    public Guid Id { get; set; }
    public required string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public required string Action { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public Guid PerformedByUserId { get; set; }
    public string? PerformedByUserName { get; set; }
    public DateTime PerformedAt { get; set; }
    public Guid? BranchId { get; set; }
    public string? MetadataJson { get; set; }
}
