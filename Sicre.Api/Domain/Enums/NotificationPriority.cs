using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Domain.Enums;

public enum NotificationPriority
{
    [Display(Name = "Baja")]
    Low = 1,

    [Display(Name = "Normal")]
    Normal = 2,

    [Display(Name = "Alta")]
    High = 3,

    [Display(Name = "Crítica")]
    Critical = 4,
}
