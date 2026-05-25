using FluentValidation;
using Sicre.Api.Features.ReportInstances.Dtos.Requests;

namespace Sicre.Api.Features.ReportInstances.Validators;

public class CreateManualReportInstanceRequestValidator
    : AbstractValidator<CreateManualReportInstanceRequest>
{
    public CreateManualReportInstanceRequestValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty().WithMessage("El reporte es requerido.");

        RuleFor(x => x.DueDate)
            .NotEmpty()
            .WithMessage("La fecha de vencimiento es requerida.");

        RuleFor(x => x.ManualActivationReason)
            .NotEmpty()
            .WithMessage("El motivo de activación manual es requerido.");
    }
}
