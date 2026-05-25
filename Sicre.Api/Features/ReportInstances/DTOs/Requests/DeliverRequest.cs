namespace Sicre.Api.Features.ReportInstances.Dtos.Requests;

public class DeliverRequest
{
    public DateOnly? SentDate { get; set; }
    public string? DelayReason { get; set; }
}
