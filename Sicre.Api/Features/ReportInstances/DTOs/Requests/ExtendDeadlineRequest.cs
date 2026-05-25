using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Features.ReportInstances.Dtos.Requests;

public class ExtendDeadlineRequest
{
    [Required]
    public DateOnly NewDueDate { get; set; }

    [Required]
    public required string Reason { get; set; }
}
