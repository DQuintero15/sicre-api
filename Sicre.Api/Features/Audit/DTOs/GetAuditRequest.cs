namespace Sicre.Api.Features.Audit.DTOs;

public class GetAuditRequest
{
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
