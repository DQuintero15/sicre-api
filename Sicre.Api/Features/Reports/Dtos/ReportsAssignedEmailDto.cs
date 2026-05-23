using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Features.Reports.Dtos;

public class ReportsAssignedEmailDto
{
    public required string UserName { get; set; }
    public required string Role { get; set; }
    public required string ControlEntityAbbreviation { get; set; }
    public required string ControlEntityName { get; set; }
    public required string ReportCode { get; set; }
    public required string ReportName { get; set; }
    public string? BranchName { get; set; }
    public int TotalReports { get; set; }
    public int TotalInstances { get; set; }
    public required List<ReportInstanceSummaryEmailDto> Instances { get; set; }
}

public class ReportInstanceSummaryEmailDto
{
    public Guid Id { get; set; }
    public required string PeriodName { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public ReportStatus Status { get; set; }
}
