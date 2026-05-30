using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Features.ReportInstances.Dtos.Responses;

public class ReportInstanceSummaryResponse
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public string? ReportCode { get; set; }
    public string? ReportName { get; set; }
    public required string PeriodName { get; set; }
    public int PeriodYear { get; set; }
    public int? PeriodMonth { get; set; }
    public DateOnly DueDate { get; set; }
    public ReportStatus Status { get; set; }
    public DateOnly? EventDate { get; set; }
    public DateTime? SentDate { get; set; }
    public Guid ResponsibleUserId { get; set; }
    public string? ResponsibleUserName { get; set; }
    public Guid SupervisorUserId { get; set; }
    public string? SupervisorUserName { get; set; }
    public int AttachmentsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
