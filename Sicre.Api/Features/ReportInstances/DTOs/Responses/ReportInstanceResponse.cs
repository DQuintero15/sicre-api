using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Features.ReportInstances.Dtos.Responses;

public class ReportInstanceResponse
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public string? ReportCode { get; set; }
    public string? ReportName { get; set; }
    public int PeriodYear { get; set; }
    public int? PeriodMonth { get; set; }
    public required string PeriodName { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? EventDate { get; set; }
    public DateTime? SentDate { get; set; }
    public ReportStatus Status { get; set; }
    public string? DelayReason { get; set; }
    public string? Observations { get; set; }
    public string? ManualActivationReason { get; set; }
    public string? DueDateOverrideReason { get; set; }
    public Guid ResponsibleUserId { get; set; }
    public string? ResponsibleUserName { get; set; }
    public Guid SupervisorUserId { get; set; }
    public string? SupervisorUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public IReadOnlyList<ReversionResponse> Reversions { get; set; } = [];
}
