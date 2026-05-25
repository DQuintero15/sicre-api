using Sicre.Api.Domain.Enums;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Notifications.Dtos;

public class NotificationDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public NotificationType Type { get; set; }
    public NotificationSeverity? Severity { get; set; }
    public NotificationPriority Priority { get; set; }
    public bool Readed { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ReportInstanceId { get; set; }
    public string? Url { get; set; }
    public string? BranchName { get; set; }
}

public class NotificationFilterRequest : PagedRequestDto { }
