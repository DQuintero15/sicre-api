using FluentValidation;
using Sicre.Api.Features.ReportInstances.Dtos.Requests;

namespace Sicre.Api.Features.ReportInstances.Validators;

public class CreateManualReportInstanceRequestValidator
    : AbstractValidator<CreateManualReportInstanceRequest>
{
    public CreateManualReportInstanceRequestValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty().WithMessage("El reporte es requerido.");

        RuleFor(x => x.PeriodYear)
            .GreaterThan(0)
            .WithMessage("El año del período debe ser mayor a 0.");

        RuleFor(x => x.ManualActivationReason)
            .NotEmpty()
            .WithMessage("La razón de activación manual es requerida.");

        When(
            x => x.DueDateOverride.HasValue,
            () =>
            {
                RuleFor(x => x.DueDateOverrideReason)
                    .NotEmpty()
                    .WithMessage(
                        "La razón de la fecha límite personalizada es requerida cuando se especifica una fecha límite."
                    );
            }
        );
    }
}
