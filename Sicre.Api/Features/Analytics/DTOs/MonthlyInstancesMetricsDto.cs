namespace Sicre.Api.Features.Analytics.DTOs;

public class MonthlyInstancesMetricsDto
{
    public MonthMetricsDto Metrics { get; set; } = new();
    public Dictionary<int, List<CalendarInstanceDto>> DayGroups { get; set; } = new();
}

public class MonthMetricsDto
{
    public int TotalInstances { get; set; }
    public int OnTimeCount { get; set; }
    public int LateCount { get; set; }
    public int OverdueCount { get; set; }
    public int UpcomingDueCount { get; set; }
    public int PendingCount { get; set; }
    public double CompliancePercentage { get; set; }
    public double OnTimePercentage { get; set; }
}

public class CalendarInstanceDto
{
    public Guid Id { get; set; }
    public Guid? ReportId { get; set; }
    public string ReportCode { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PeriodName { get; set; } = string.Empty;
    public string Datetime { get; set; } = string.Empty;
}
