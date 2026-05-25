namespace Sicre.Api.Features.Analytics.DTOs;

public class EntityComplianceDto
{
    public string EntityName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int OnTime { get; set; }
    public int Late { get; set; }
    public int Overdue { get; set; }
    public int Pending { get; set; }
    public double OnTimeRate { get; set; }
    public double DeliveryRate { get; set; }
}
