using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Features.ReportInstances.Dtos.Requests;

public class AddNoteRequest
{
    [Required]
    [MaxLength(10000)]
    public required string Content { get; set; }
}
