using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Domain.Enums;

public enum NotificationSeverity
{
    [Display(Name = "General")]
    General = 0,

    [Display(Name = "Informativa")]
    Info = 1,

    [Display(Name = "Advertencia")]
    Warning = 2,

    [Display(Name = "Urgente")]
    Urgent = 3,

    [Display(Name = "Crítica")]
    Critical = 4,
}
