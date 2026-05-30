namespace Sicre.Api.Features.Analytics.DTOs;

public class ReversionSummaryDto
{
    public string ReportName { get; set; } = string.Empty;
    public string ReportCode { get; set; } = string.Empty;
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
