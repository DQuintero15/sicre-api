namespace Sicre.Api.Features.Analytics.DTOs;

public class StateDistributionDto
{
    public int Total { get; set; }
    public int OnTime { get; set; }
    public int Late { get; set; }
    public int Overdue { get; set; }
    public int UpcomingDue { get; set; }
    public int Pending { get; set; }
}
