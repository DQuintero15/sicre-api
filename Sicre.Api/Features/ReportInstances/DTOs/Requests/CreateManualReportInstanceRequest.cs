using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Features.ReportInstances.Dtos.Requests;

public sealed class CreateManualReportInstanceRequest
{
    [Required]
    public Guid ReportId { get; set; }

    [Required]
    public DateOnly DueDate { get; set; }

    public DateOnly? EventDate { get; set; }

    [Required]
    public string ManualActivationReason { get; set; } = string.Empty;
}
