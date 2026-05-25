using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Features.ReportInstances.Dtos.Responses;

public class ReportAttachmentResponse
{
    public Guid Id { get; set; }
    public Guid ReportInstanceId { get; set; }
    public AttachmentType Type { get; set; }
    public required string FileName { get; set; }
    public string? MimeType { get; set; }
    public string? GoogleFileId { get; set; }
    public string? WebViewLink { get; set; }
    public string? WebContentLink { get; set; }
    public string? Url { get; set; }
    public long FileSize { get; set; }
    public UploadProgress UploadProgress { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string? UploadedByUserName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
