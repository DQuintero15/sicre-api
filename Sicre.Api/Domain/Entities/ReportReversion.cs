using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Domain.Entities;

public class ReportReversion
{
    public Guid Id { get; set; }
    public Guid ReportInstanceId { get; set; }
    public ReportInstance? ReportInstance { get; set; }
    public ReportStatus PreviousStatus { get; set; }
    public ReportStatus NewStatus { get; set; }
    public required string Reason { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
