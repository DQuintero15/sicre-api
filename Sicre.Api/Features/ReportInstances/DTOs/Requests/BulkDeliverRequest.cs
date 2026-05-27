namespace Sicre.Api.Features.ReportInstances.Dtos.Requests;

public class BulkDeliverRequest
{
    public List<Guid> InstanceIds { get; set; } = [];
}
