using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Domain.Enums;

public enum NotificationType
{
    [Display(Name = "Aplicación")]
    APP = 1,

    [Display(Name = "Correo electrónico")]
    EMAIL = 3,
}
