using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Domain.Enums;

public enum ReportDueDateRuleType
{
    [Display(Name = "Día fijo del mes")]
    DayOfMonth = 1,

    [Display(Name = "Último día del mes")]
    LastDayOfMonth = 2,

    [Display(Name = "Fecha fija anual")]
    FixedDate = 3,

    [Display(Name = "Fechas fijas del año")]
    FixedDates = 4,

    [Display(Name = "Fecha manual requerida")]
    ManualDateRequired = 5,
}
