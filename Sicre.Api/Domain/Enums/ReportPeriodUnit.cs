using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Domain.Enums;

public enum ReportPeriodUnit
{
    [Display(Name = "Mes")]
    Month = 1,

    [Display(Name = "Trimestre")]
    Quarter = 2,

    [Display(Name = "Semestre")]
    Semester = 3,

    [Display(Name = "Año")]
    Year = 4,
}
