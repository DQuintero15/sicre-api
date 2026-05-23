using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Domain.Enums;

public enum Role
{
    [Display(Name = "Administrador")]
    Administrator = 1,

    [Display(Name = "Responsable del Reporte")]
    ReportResponsible = 2,

    [Display(Name = "Supervisor de Cumplimiento")]
    ComplianceSupervisor = 3,

    [Display(Name = "Auditor")]
    Auditor = 4,
}
