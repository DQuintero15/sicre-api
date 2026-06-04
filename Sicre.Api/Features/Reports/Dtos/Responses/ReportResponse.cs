using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Features.Reports.Dtos.Responses;

public class ReportResponse
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public Guid ControlEntityId { get; set; }
    public string? ControlEntityName { get; set; }
    public Guid? ProcessId { get; set; }
    public string? ProcessName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? LegalBasis { get; set; }
    public string? Description { get; set; }
    public ReportFrequency Frequency { get; set; }
    public ReportGenerationMode GenerationMode { get; set; }
    public ReportDueDateRuleType DueDateRuleType { get; set; }
    public int? DueDateDay { get; set; }
    public int? DueDateMonth { get; set; }
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
    public string? SenderResponsibleUserName { get; set; }
    public Guid EntityUploadResponsibleUserId { get; set; }
    public string? EntityUploadResponsibleUserName { get; set; }
    public Guid FollowUpLeaderUserId { get; set; }
    public string? FollowUpLeaderUserName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
