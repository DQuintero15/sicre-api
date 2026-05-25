namespace Sicre.Api.Features.Analytics.DTOs;

public class ComplianceTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int Total { get; set; }
    public int OnTime { get; set; }
    public int Late { get; set; }
    public int Overdue { get; set; }
    public int Pending { get; set; }
    public double OnTimePercentage { get; set; }
    public double LatePercentage { get; set; }
    public double OverduePercentage { get; set; }
    public double PendingPercentage { get; set; }
}
