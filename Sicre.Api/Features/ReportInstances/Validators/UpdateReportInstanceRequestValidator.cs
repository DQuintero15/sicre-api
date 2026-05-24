using FluentValidation;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.ReportInstances.Dtos.Requests;

namespace Sicre.Api.Features.ReportInstances.Validators;

public class UpdateReportInstanceRequestValidator : AbstractValidator<UpdateReportInstanceRequest>
{
    public UpdateReportInstanceRequestValidator()
    {
        When(
            x => x.DueDate.HasValue,
            () =>
            {
                RuleFor(x => x.DueDateOverrideReason)
                    .NotEmpty()
                    .WithMessage(
                        "La razón de la fecha límite personalizada es requerida cuando se cambia la fecha límite."
                    );
            }
        );

        When(
            x => x.Status == ReportStatus.SentOnTime || x.Status == ReportStatus.SentLate,
            () =>
            {
                RuleFor(x => x.SentDate)
                    .NotNull()
                    .WithMessage("La fecha de envío es requerida cuando se marca como enviado.");
            }
        );
    }
}
