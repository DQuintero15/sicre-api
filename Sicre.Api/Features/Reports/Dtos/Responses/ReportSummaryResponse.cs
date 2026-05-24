using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.ReportInstances.Dtos.Responses;

namespace Sicre.Api.Features.Reports.Dtos.Responses;

public class ReportSummaryResponse
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public Guid ControlEntityId { get; set; }
    public string? ControlEntityName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public Guid? ProcessId { get; set; }
    public string? ProcessName { get; set; }
    public ReportFrequency Frequency { get; set; }
    public ReportGenerationMode GenerationMode { get; set; }
    public ReportDueDateRuleType DueDateRuleType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public int TotalInstances { get; set; }
    public int PendingInstances { get; set; }
    public int OverdueInstances { get; set; }
    public int CompletedInstances { get; set; }
    public List<ReportInstanceSummaryResponse> Instances { get; set; } = [];
}
