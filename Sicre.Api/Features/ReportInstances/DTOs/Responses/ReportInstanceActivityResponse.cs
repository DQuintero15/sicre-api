namespace Sicre.Api.Features.ReportInstances.Dtos.Responses;

public class ReportInstanceActivityResponse
{
    public Guid Id { get; set; }
    public required string Action { get; set; }
    public required string HumanReadable { get; set; }
    public string? PerformedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReportInstanceAuditEntryResponse
{
    public Guid Id { get; set; }
    public required string Action { get; set; }
    public required string HumanReadable { get; set; }
    public string? PerformedByUserName { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
