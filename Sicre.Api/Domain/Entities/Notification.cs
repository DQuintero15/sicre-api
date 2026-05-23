using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required NotificationType Type { get; set; }
    public NotificationSeverity? Severity { get; set; }
    public NotificationPriority Priority { get; set; }
    public bool Readed { get; set; } = false;
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ReportInstanceId { get; set; }
    public ReportInstance? ReportInstance { get; set; }
    public string? Url { get; set; }
    public User? User { get; set; }
}
