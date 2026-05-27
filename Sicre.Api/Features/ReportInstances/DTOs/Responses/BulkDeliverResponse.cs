namespace Sicre.Api.Features.ReportInstances.Dtos.Responses;

public class BulkDeliverResponse
{
    public List<BulkDeliverItemResult> Results { get; set; } = [];
    public int SuccessCount => Results.Count(r => r.Success);
    public int FailureCount => Results.Count(r => !r.Success);
}

public class BulkDeliverItemResult
{
    public Guid InstanceId { get; set; }
    public bool Success { get; set; }
    public string? Reason { get; set; }
}
