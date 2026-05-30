using Sicre.Api.Features.Analytics.DTOs;

namespace Sicre.Api.Features.Analytics.Services;

public interface IAnalyticsExportService
{
    Task<ExportPdfResult> ExportAsync(AnalyticsFilterRequest filter, Guid userId, IList<string> userRoles);
}
