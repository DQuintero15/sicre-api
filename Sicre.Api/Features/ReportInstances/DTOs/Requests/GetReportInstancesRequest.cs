using Sicre.Api.Domain.Enums;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.ReportInstances.Dtos.Requests;

public class GetReportInstancesRequest : PagedRequestDto
{
    public Guid? ReportId { get; set; }
    public Guid? ControlEntityId { get; set; }
    public ReportStatus? Status { get; set; }
    public int? PeriodYear { get; set; }
    public int? PeriodMonth { get; set; }
}
