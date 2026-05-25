using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Features.Reports.Dtos.Requests;

public class CreateReportRequest
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public Guid ControlEntityId { get; set; }
    public Guid? ProcessId { get; set; }
    public Guid? BranchId { get; set; }
    public string? LegalBasis { get; set; }
    public ReportFrequency Frequency { get; set; }
    public ReportGenerationMode GenerationMode { get; set; }
    public ReportDueDateRuleType DueDateRuleType { get; set; }

    // DayOfMonth + FixedDate
    public int? DueDateDay { get; set; }

    // FixedDate only
    public int? DueDateMonth { get; set; }

    // FixedDates: JSON [{month, day}]
    public string? DueDateDatesDefinition { get; set; }
    public string? OriginalDueDateText { get; set; }

    public int AlertEarlyDays { get; set; }
    public int AlertFollowUpDays { get; set; }
    public int AlertCriticalDays { get; set; }
    public List<ReportFormatType> FormatTypes { get; set; } = [];
    public string? InstructionsUrl { get; set; }
    public string? TemplateFileUrl { get; set; }
    public string? NotificationEmails { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Guid SenderResponsibleUserId { get; set; }
    public Guid EntityUploadResponsibleUserId { get; set; }
    public Guid FollowUpLeaderUserId { get; set; }
}
