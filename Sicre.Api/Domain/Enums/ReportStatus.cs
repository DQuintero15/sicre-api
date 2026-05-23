using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Domain.Enums;

public enum ReportStatus
{
    [Display(Name = "Pendiente")]
    Pending = 1,

    [Display(Name = "Enviado a tiempo")]
    SentOnTime = 2,

    [Display(Name = "Enviado tarde")]
    SentLate = 3,

    [Display(Name = "Vencido")]
    Overdue = 4,
}
