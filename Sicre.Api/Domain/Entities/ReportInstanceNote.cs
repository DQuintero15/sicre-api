namespace Sicre.Api.Domain.Entities;

public class ReportInstanceNote
{
    public Guid Id { get; set; }
    public Guid ReportInstanceId { get; set; }
    public ReportInstance? ReportInstance { get; set; }

    public required string Content { get; set; }

    public Guid AuthorUserId { get; set; }
    public User? AuthorUser { get; set; }
    public string AuthorRole { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
