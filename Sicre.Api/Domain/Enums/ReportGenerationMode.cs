using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Domain.Enums;

public enum ReportGenerationMode
{
    [Display(Name = "Automático")]
    Automatic = 1,

    [Display(Name = "Manual por evento")]
    ManualEventBased = 2,

    [Display(Name = "Manual por solicitud de entidad")]
    ManualRequestedByEntity = 3,

    [Display(Name = "Manual por resolución")]
    ManualResolutionBased = 4,
}
