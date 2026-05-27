namespace Sicre.Api.Domain.Entities;

public class ReportInstanceNote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReportInstanceId { get; set; }
    public ReportInstance? ReportInstance { get; set; }
    public required string Content { get; set; }
    public bool IsVisibleToResponsible { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
