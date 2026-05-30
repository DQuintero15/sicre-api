using Sicre.Api.Features.Analytics.DTOs;

namespace Sicre.Api.Shared.Reports;

public sealed class MonthlyReportData
{
    public required string PeriodLabel { get; init; }
    public required int PeriodYear { get; init; }
    public required int PeriodMonth { get; init; }
    public required string GeneratedAt { get; init; }
    public required StateDistributionDto StateDistribution { get; init; }
    public required List<ComplianceTrendDto> Trend { get; init; }
    public required List<EntityComplianceDto> ByEntity { get; init; }
    public required List<ResponsibleComplianceDto> ByResponsible { get; init; }
    public required List<BranchComplianceDto> ByBranch { get; init; }
    public byte[]? LogoLlanogas { get; init; }
    public byte[]? LogoCusianagas { get; init; }
}
