namespace Sicre.Api.Features.ReportInstances.Dtos.Requests;

public class CreateNoteRequest
{
    public required string Content { get; set; }
    public bool IsVisibleToResponsible { get; set; }
}
