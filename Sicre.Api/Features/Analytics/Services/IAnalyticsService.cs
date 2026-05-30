using Sicre.Api.Features.Analytics.DTOs;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Analytics.Services;

public interface IAnalyticsService
{
    Task<ApiResponse<StateDistributionDto>> GetStateDistributionAsync(
        Guid userId,
        IList<string> userRoles,
        AnalyticsFilterRequest filter
    );

    Task<ApiResponse<List<ComplianceTrendDto>>> GetComplianceTrendAsync(
        Guid userId,
        IList<string> userRoles,
        AnalyticsFilterRequest filter
    );

    Task<ApiResponse<List<EntityComplianceDto>>> GetComplianceByEntityAsync(
        Guid userId,
        IList<string> userRoles,
        AnalyticsFilterRequest filter
    );

    Task<ApiResponse<List<ResponsibleComplianceDto>>> GetComplianceByResponsibleAsync(
        Guid userId,
        IList<string> userRoles,
        AnalyticsFilterRequest filter
    );

    Task<ApiResponse<MonthlyInstancesMetricsDto>> GetMonthlyInstancesMetricsAsync(
        Guid userId,
        IList<string> userRoles,
        int month,
        int year
    );

    Task<ApiResponse<List<BranchComplianceDto>>> GetComplianceByBranchAsync(
        Guid userId,
        IList<string> userRoles,
        AnalyticsFilterRequest filter
    );

    Task<ApiResponse<List<ReversionSummaryDto>>> GetReversionsByPeriodAsync(
        Guid userId,
        IList<string> userRoles,
        AnalyticsFilterRequest filter
    );
}
