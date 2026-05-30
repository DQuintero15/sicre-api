using FluentValidation;
using Sicre.Api.Features.Reports.Dtos.Requests;

namespace Sicre.Api.Features.Reports.Validators;

public class UpdateReportRequestValidator : AbstractValidator<UpdateReportRequest>
{
    public UpdateReportRequestValidator()
    {
        When(
            x => x.Code != null,
            () => RuleFor(x => x.Code).NotEmpty().WithMessage("El código no puede estar vacío.")
        );

        When(
            x => x.Name != null,
            () => RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre no puede estar vacío.")
        );

        When(
            x => x.AlertEarlyDays.HasValue,
            () =>
                RuleFor(x => x.AlertEarlyDays)
                    .GreaterThan(0)
                    .WithMessage("Los días de alerta temprana deben ser mayores a 0.")
        );

        When(
            x => x.AlertFollowUpDays.HasValue,
            () =>
                RuleFor(x => x.AlertFollowUpDays)
                    .GreaterThan(0)
                    .WithMessage("Los días de seguimiento deben ser mayores a 0.")
        );

        When(
            x => x.AlertCriticalDays.HasValue,
            () =>
                RuleFor(x => x.AlertCriticalDays)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Los días de alerta crítica no pueden ser negativos.")
        );
    }
}
