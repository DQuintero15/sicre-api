namespace Sicre.Api.Features.ReportInstances.Dtos.Responses;

public class NoteResponse
{
    public Guid Id { get; set; }
    public Guid ReportInstanceId { get; set; }
    public required string Content { get; set; }
    public bool IsVisibleToResponsible { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}
