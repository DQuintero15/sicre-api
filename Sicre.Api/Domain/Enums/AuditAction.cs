namespace Sicre.Api.Domain.Enums;

public static class AuditAction
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Deliver = "Deliver";
    public const string Revert = "Revert";
    public const string Deactivate = "Deactivate";
    public const string BulkDeliver = "BulkDeliver";
    public const string Reassign = "Reassign";
    public const string UpdateSensitiveField = "UpdateSensitiveField";
    public const string MonthlyReportSent = "MonthlyReportSent";
}
