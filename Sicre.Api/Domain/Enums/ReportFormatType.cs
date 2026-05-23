using System.ComponentModel.DataAnnotations;

namespace Sicre.Api.Domain.Enums;

public enum ReportFormatType
{
    [Display(Name = "Cualquiera")]
    Any = 0,

    [Display(Name = "PDF")]
    PDF = 1,

    [Display(Name = "Hoja de cálculo")]
    Spreadsheet = 2,

    [Display(Name = "Archivo comprimido")]
    Archive = 3,

    [Display(Name = "Plataforma web")]
    WebPlatform = 4,

    [Display(Name = "Datos estructurados")]
    StructuredData = 5,
}
