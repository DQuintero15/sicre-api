using FluentValidation;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.Reports.Dtos.Requests;

namespace Sicre.Api.Features.Reports.Validators;

public class CreateReportRequestValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("El código es requerido.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre es requerido.");
        RuleFor(x => x.ControlEntityId)
            .NotEmpty()
            .WithMessage("La entidad de control es requerida.");
        RuleFor(x => x.Frequency).IsInEnum().WithMessage("La frecuencia es inválida.");
        RuleFor(x => x.GenerationMode).IsInEnum().WithMessage("El modo de generación es inválido.");
        RuleFor(x => x.DueDateRuleType)
            .IsInEnum()
            .WithMessage("El tipo de regla de vencimiento es inválido.");
        RuleFor(x => x.AlertEarlyDays)
            .GreaterThan(0)
            .WithMessage("Los días de alerta temprana deben ser mayores a 0.");
        RuleFor(x => x.AlertFollowUpDays)
            .GreaterThan(0)
            .WithMessage("Los días de seguimiento deben ser mayores a 0.");
        RuleFor(x => x.AlertCriticalDays)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Los días de alerta crítica no pueden ser negativos.");
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("La fecha de inicio es requerida.");

        When(
            x => x.DueDateRuleType == ReportDueDateRuleType.DayNumberOfPeriod,
            () =>
            {
                RuleFor(x => x.DueDateDayNumber)
                    .NotNull()
                    .WithMessage("El número de día del periodo es requerido.")
                    .InclusiveBetween(1, 31)
                    .WithMessage("El número de día debe estar entre 1 y 31.");
            }
        );

        When(
            x => x.DueDateRuleType == ReportDueDateRuleType.DaysAfterPeriodEnd,
            () =>
            {
                RuleFor(x => x.DueDateDaysToAdd)
                    .NotNull()
                    .WithMessage("Los días a agregar son requeridos.")
                    .GreaterThan(0)
                    .WithMessage("Los días a agregar deben ser mayores a 0.");
            }
        );

        When(
            x => x.DueDateRuleType == ReportDueDateRuleType.DaysAfterEvent,
            () =>
            {
                RuleFor(x => x.DueDateDaysToAdd)
                    .NotNull()
                    .WithMessage("Los días a agregar son requeridos.")
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Los días a agregar deben ser mayores o iguales a 0.");
            }
        );

        When(
            x => x.DueDateRuleType == ReportDueDateRuleType.FixedDateSet,
            () =>
            {
                RuleFor(x => x)
                    .Must(x =>
                        !string.IsNullOrWhiteSpace(x.DueDateFixedDatesDefinition)
                        || (x.DueDateFixedMonth.HasValue && x.DueDateFixedDay.HasValue)
                    )
                    .WithMessage(
                        "FixedDateSet requiere DueDateFixedDatesDefinition (JSON) o DueDateFixedMonth + DueDateFixedDay."
                    );

                RuleFor(x => x.DueDateFixedDatesDefinition)
                    .Must(BeValidJson)
                    .When(x => !string.IsNullOrWhiteSpace(x.DueDateFixedDatesDefinition))
                    .WithMessage("La definición de fechas fijas debe ser JSON válido.");
            }
        );

        When(
            x => x.DueDateRuleType == ReportDueDateRuleType.DateRangeSet,
            () =>
            {
                RuleFor(x => x.DueDateRangesDefinition)
                    .NotEmpty()
                    .WithMessage("La definición de rangos de fechas es requerida.")
                    .Must(BeValidJson)
                    .WithMessage("La definición de rangos de fechas debe ser JSON válido.");
            }
        );

        When(
            x => x.DueDateRuleType == ReportDueDateRuleType.SpecificDate,
            () =>
            {
                RuleFor(x => x.DueDateSpecificDate)
                    .NotNull()
                    .WithMessage("La fecha específica de vencimiento es requerida.");
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
