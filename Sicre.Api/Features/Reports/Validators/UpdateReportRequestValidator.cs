using FluentValidation;
using Sicre.Api.Domain.Enums;
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
            x => x.Frequency.HasValue,
            () => RuleFor(x => x.Frequency).IsInEnum().WithMessage("La frecuencia es inválida.")
        );

        When(
            x => x.GenerationMode.HasValue,
            () =>
                RuleFor(x => x.GenerationMode)
                    .IsInEnum()
                    .WithMessage("El modo de generación es inválido.")
        );

        When(
            x => x.DueDateRuleType.HasValue,
            () =>
                RuleFor(x => x.DueDateRuleType)
                    .IsInEnum()
                    .WithMessage("El tipo de regla de vencimiento es inválido.")
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

        When(
            x => x.DueDateRuleType == ReportDueDateRuleType.DayOfMonth,
            () =>
            {
                RuleFor(x => x.DueDateDay)
                    .NotNull()
                    .WithMessage("El día del mes es requerido.")
                    .InclusiveBetween(1, 31)
                    .WithMessage("El día debe estar entre 1 y 31.");
            }
        );

        When(
            x => x.DueDateRuleType == ReportDueDateRuleType.FixedDate,
            () =>
            {
                RuleFor(x => x.DueDateDay)
                    .NotNull()
                    .WithMessage("El día de la fecha fija es requerido.")
                    .InclusiveBetween(1, 31)
                    .WithMessage("El día debe estar entre 1 y 31.");
                RuleFor(x => x.DueDateMonth)
                    .NotNull()
                    .WithMessage("El mes de la fecha fija es requerido.")
                    .InclusiveBetween(1, 12)
                    .WithMessage("El mes debe estar entre 1 y 12.");
            }
        );

        When(
            x => x.DueDateRuleType == ReportDueDateRuleType.FixedDates,
            () =>
            {
                RuleFor(x => x.DueDateDatesDefinition)
                    .NotEmpty()
                    .WithMessage("La definición de fechas es requerida.")
                    .Must(BeValidJson)
                    .WithMessage(
                        "La definición de fechas debe ser JSON válido con formato [{\"month\":N,\"day\":N}]."
                    );
            }
        );
    }

    private static bool BeValidJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        try
        {
            System.Text.Json.JsonDocument.Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
