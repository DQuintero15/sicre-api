using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Domain.Enums;

public enum AttachmentType
{
    [Display(Name = "Informe final")]
    FinalReport = 1,

    [Display(Name = "Soporte de envío")]
    SubmissionEvidence = 2,

    [Display(Name = "Soporte de prórroga")]
    DeadlineExtensionEvidence = 3,

    [Display(Name = "Soporte de reversión")]
    ReversionEvidence = 4,
}
