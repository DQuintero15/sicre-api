using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.Reports.Dtos;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Reports.Dtos.Requests;

public class GetReportsRequest : PagedRequestDto
{
    public string? Search { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public Guid? ControlEntityId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ProcessId { get; set; }
    public ReportFrequency? Frequency { get; set; }
    public ReportGenerationMode? GenerationMode { get; set; }
    public ReportDueDateRuleType? DueDateRuleType { get; set; }
    public bool? IsActive { get; set; }
    public ReportSortBy? SortBy { get; set; }
    public bool? SortDescending { get; set; }
}
