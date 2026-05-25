using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Features.ReportInstances.Dtos.Requests;

public class AddFileAttachmentRequest
{
    public AttachmentType Type { get; set; } = AttachmentType.FinalReport;
}
