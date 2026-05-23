using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Domain.Entities;

public class Report
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public Guid ControlEntityId { get; set; }
    public ControlEntity? ControlEntity { get; set; }
    public Guid? ProcessId { get; set; }
    public Process? Process { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string? LegalBasis { get; set; }
    public ReportFrequency Frequency { get; set; }
    public ReportGenerationMode GenerationMode { get; set; }
    public ReportDueDateRuleType DueDateRuleType { get; set; }
    public ReportPeriodUnit? DueDatePeriodUnit { get; set; }
    public int? DueDateDayNumber { get; set; }
    public int? DueDateDaysToAdd { get; set; }
    public int? DueDateMonthOffset { get; set; }
    public int? DueDateYearOffset { get; set; }
    public int? DueDateFixedMonth { get; set; }
    public int? DueDateFixedDay { get; set; }
    public DateOnly? DueDateSpecificDate { get; set; }
    public string? DueDateFixedDatesDefinition { get; set; }
    public string? DueDateRangesDefinition { get; set; }
    public string? DueDateExceptionsDefinition { get; set; }
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
    public User? SenderResponsibleUser { get; set; }
    public Guid EntityUploadResponsibleUserId { get; set; }
    public User? EntityUploadResponsibleUser { get; set; }
    public Guid FollowUpLeaderUserId { get; set; }
    public User? FollowUpLeaderUser { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ReportInstance> Instances { get; set; } = [];
}
