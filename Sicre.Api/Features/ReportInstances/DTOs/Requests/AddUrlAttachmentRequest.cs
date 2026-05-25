using System.ComponentModel.DataAnnotations;
using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Features.ReportInstances.Dtos.Requests;

public class AddUrlAttachmentRequest
{
    [Required]
    public AttachmentType Type { get; set; } = AttachmentType.SubmissionEvidence;

    [Required]
    public required string Name { get; set; }

    [Required]
    public required string Url { get; set; }
}
