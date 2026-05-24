using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Features.ReportInstances.Dtos.Requests;

public class UpdateReportInstanceRequest
{
    public DateOnly? DueDate { get; set; }
    public string? DueDateOverrideReason { get; set; }
    public ReportStatus? Status { get; set; }
    public DateTime? SentDate { get; set; }
    public string? DelayReason { get; set; }
    public string? Observations { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public Guid? SupervisorUserId { get; set; }
}
