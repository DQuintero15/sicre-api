using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Domain.Entities;

public class ReportAttachment
{
    public Guid Id { get; set; }
    public Guid ReportInstanceId { get; set; }
    public ReportInstance? ReportInstance { get; set; }
    public AttachmentType Type { get; set; }
    public required string FileName { get; set; }
    public string? MimeType { get; set; }
    public string? GoogleFileId { get; set; }
    public string? WebViewLink { get; set; }
    public string? WebContentLink { get; set; }
    public long FileSize { get; set; }
    public Guid UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
