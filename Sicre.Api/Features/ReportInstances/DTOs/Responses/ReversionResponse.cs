using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Features.ReportInstances.Dtos.Responses;

public class ReversionResponse
{
    public Guid Id { get; set; }
    public ReportStatus PreviousStatus { get; set; }
    public ReportStatus NewStatus { get; set; }
    public required string Reason { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}
