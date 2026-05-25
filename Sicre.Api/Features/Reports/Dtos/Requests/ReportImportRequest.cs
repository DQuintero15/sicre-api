namespace Sicre.Api.Features.Reports.Dtos.Requests;

public class ReportImportRequest
{
    public string? SourceFile { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public bool GenerateInitialInstances { get; set; } = true;
    public bool ValidateOnly { get; set; }
    public string? BusinessDecision { get; set; }
    public List<ImportReportItem> Reports { get; set; } = [];
}

public class ImportReportItem
{
    public int SourceRowNumber { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string ControlEntityName { get; set; } = "";
    public string? ControlEntityNit { get; set; }
    public string? ControlEntityWebsite { get; set; }
    public string? ProcessName { get; set; }
    public string? BranchName { get; set; }
    public string? LegalBasis { get; set; }
    public string? EntityStatus { get; set; }
    public string? Description { get; set; }

    // Enum strings: "Monthly", "Automatic", "DayOfMonth", etc.
    public string Frequency { get; set; } = "";
    public string GenerationMode { get; set; } = "";
    public string DueDateRuleType { get; set; } = "";

    public int? DueDateDay { get; set; }
    public int? DueDateMonth { get; set; }
    public List<FixedDateEntry>? DueDateDates { get; set; }
    public int? DueDateDaysToAdd { get; set; }

    public string StartDate { get; set; } = "";
    public string? EndDate { get; set; }

    public int? AlertEarlyDays { get; set; }
    public int? AlertFollowUpDays { get; set; }
    public int? AlertCriticalDays { get; set; }

    public List<string> FormatTypes { get; set; } = [];
    public string? InstructionsUrl { get; set; }
    public string? TemplateFileUrl { get; set; }

    public string? SenderResponsibleRole { get; set; }
    public string? SenderResponsibleName { get; set; }
    public string? SenderResponsibleEmail { get; set; }
    public string? EntityUploadResponsibleName { get; set; }
    public string? EntityUploadResponsibleEmail { get; set; }
    public string? FollowUpLeaderName { get; set; }
    public string? FollowUpLeaderEmail { get; set; }

    // Semicolon-separated email string
    public string? NotificationEmails { get; set; }
    public string? OriginalDueDateText { get; set; }

    public bool IsActive { get; set; } = true;

    // Raw source fields — ignored during import
    public string? ResponsibleDeliveryRaw { get; set; }
    public string? DueDateRaw { get; set; }
    public string? AlertRaw1 { get; set; }
    public string? AlertRaw2 { get; set; }
    public string? AlertRaw3 { get; set; }
    public string? OverdueAlertRaw { get; set; }
    public List<object>? ValidationErrors { get; set; }
}

public class FixedDateEntry
{
    public int Month { get; set; }
    public int Day { get; set; }
}
