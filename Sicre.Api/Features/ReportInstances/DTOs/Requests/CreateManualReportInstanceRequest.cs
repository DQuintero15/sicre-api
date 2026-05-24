namespace Sicre.Api.Features.ReportInstances.Dtos.Requests;

public class CreateManualReportInstanceRequest
{
    public Guid ReportId { get; set; }
    public int PeriodYear { get; set; }
    public int? PeriodMonth { get; set; }
    public DateOnly? EventDate { get; set; }
    public DateOnly? DueDateOverride { get; set; }
    public string? DueDateOverrideReason { get; set; }
    public required string ManualActivationReason { get; set; }
}
