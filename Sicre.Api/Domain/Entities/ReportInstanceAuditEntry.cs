namespace Sicre.Api.Domain.Entities;

public class ReportInstanceAuditEntry
{
    public Guid Id { get; set; }
    public Guid ReportInstanceId { get; set; }
    public ReportInstance? ReportInstance { get; set; }

    public required string Action { get; set; }

    public Guid PerformedByUserId { get; set; }
    public User? PerformedByUser { get; set; }

    public string? Details { get; set; }

    public required string HumanReadable { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
