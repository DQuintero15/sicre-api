using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Domain.Enums;

public enum ReportFrequency
{
    [Display(Name = "Mensual")]
    Monthly = 1,

    [Display(Name = "Mensual anticipado")]
    MonthlyAnticipated = 2,

    [Display(Name = "Trimestral")]
    Quarterly = 3,

    [Display(Name = "Semestral")]
    SemiAnnual = 4,

    [Display(Name = "Anual")]
    Annual = 5,

    [Display(Name = "Eventual")]
    Eventual = 6,
}
