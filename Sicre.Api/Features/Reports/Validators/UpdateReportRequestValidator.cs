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
                    .Must(BeValidFixedDatesJson)
                    .WithMessage(
                        "La definición de fechas debe ser JSON válido con formato [{\"month\":N,\"day\":N}] sin fechas duplicadas."
                    );
            }
        );

        When(
            x => x.NotificationEmails != null,
            () =>
                RuleFor(x => x.NotificationEmails)
                    .Must(BeValidEmailList)
                    .WithMessage(
                        "NotificationEmails debe ser una lista de emails válidos separados por coma."
                    )
        );
    }

    private static bool BeValidFixedDatesJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        try
        {
            var doc = System.Text.Json.JsonDocument.Parse(value);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                return false;

            var seen = new HashSet<(int month, int day)>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (
                    !element.TryGetProperty("month", out var monthEl)
                    || !element.TryGetProperty("day", out var dayEl)
                )
                    return false;

                var month = monthEl.GetInt32();
                var day = dayEl.GetInt32();

                if (month < 1 || month > 12 || day < 1 || day > 31)
                    return false;

                if (!seen.Add((month, day)))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static readonly System.Text.RegularExpressions.Regex EmailRegex =
        new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            System.Text.RegularExpressions.RegexOptions.Compiled
                | System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

    private static bool BeValidEmailList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var emails = value.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
        return emails.All(e => EmailRegex.IsMatch(e.Trim()));
    }
}
