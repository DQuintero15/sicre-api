namespace Sicre.Api.Features.Analytics.DTOs;

public class AnalyticsFilterRequest
{
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Guid? ControlEntityId { get; set; }
    public Guid? ResponsibleUserId { get; set; }
}
