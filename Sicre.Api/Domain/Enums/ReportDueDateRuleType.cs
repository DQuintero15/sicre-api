using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Domain.Enums;

public enum ReportDueDateRuleType
{
    [Display(Name = "Día N del periodo")]
    DayNumberOfPeriod = 1,

    [Display(Name = "Último día del periodo")]
    LastDayOfPeriod = 2,

    [Display(Name = "Días después del cierre del periodo")]
    DaysAfterPeriodEnd = 3,

    [Display(Name = "Días después de un evento")]
    DaysAfterEvent = 4,

    [Display(Name = "Fechas fijas")]
    FixedDateSet = 5,

    [Display(Name = "Rango de fechas")]
    DateRangeSet = 6,

    [Display(Name = "Fecha específica")]
    SpecificDate = 7,

    [Display(Name = "Fecha manual requerida")]
    ManualDateRequired = 8,
}
