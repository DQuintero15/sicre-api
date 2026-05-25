using System.Text.Json;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Features.Reports.Services;

// ─── Support types ─────────────────────────────────────────────────────────────

public record ReportInstanceCandidate(
    Guid ReportId,
    int PeriodYear,
    int? PeriodMonth,
    string PeriodName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly DueDate,
    DateOnly? EventDate
);

// ─── Interface ──────────────────────────────────────────────────────────────────

public interface IReportInstanceGenerator
{
    /// <summary>
    /// Returns the next due-date candidate for the Hangfire rolling job.
    /// Returns null if the report is not auto-generable or nothing remains in the horizon.
    /// </summary>
    ReportInstanceCandidate? GetNextCandidate(
        Report report,
        ReportInstance? latestInstance,
        DateOnly goLiveDate
    );

    /// <summary>
    /// Returns all candidates whose DueDate falls inside [windowStart, windowEnd].
    /// Used for the 12-month initial projection.
    /// </summary>
    IReadOnlyList<ReportInstanceCandidate> GetCandidatesInWindow(
        Report report,
        DateOnly windowStart,
        DateOnly windowEnd,
        DateOnly goLiveDate
    );
}

// ─── Implementation ─────────────────────────────────────────────────────────────

public class ReportInstanceGenerator(ILogger<ReportInstanceGenerator> logger)
    : IReportInstanceGenerator
{
    private static readonly string[] SpanishMonths =
    [
        "",
        "Enero",
        "Febrero",
        "Marzo",
        "Abril",
        "Mayo",
        "Junio",
        "Julio",
        "Agosto",
        "Septiembre",
        "Octubre",
        "Noviembre",
        "Diciembre",
    ];

    // ── Public API ────────────────────────────────────────────────────────────

    public ReportInstanceCandidate? GetNextCandidate(
        Report report,
        ReportInstance? latestInstance,
        DateOnly goLiveDate
    )
    {
        if (!IsAutoGenerable(report))
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveStart = ComputeEffectiveStart(report, goLiveDate);
        var horizon = GetRollingHorizon(report.Frequency, today);

        // Search from the later of effectiveStart or the day after the last known due date
        var searchFrom = latestInstance is null
            ? effectiveStart
            : Max(effectiveStart, latestInstance.DueDate.AddDays(1));

        return GenerateCandidates(report, searchFrom, horizon)
            .OrderBy(c => c.DueDate)
            .FirstOrDefault();
    }

    public IReadOnlyList<ReportInstanceCandidate> GetCandidatesInWindow(
        Report report,
        DateOnly windowStart,
        DateOnly windowEnd,
        DateOnly goLiveDate
    )
    {
        if (!IsAutoGenerable(report))
            return [];

        var effectiveStart = ComputeEffectiveStart(report, goLiveDate);
        var from = Max(windowStart, effectiveStart);

        return GenerateCandidates(report, from, windowEnd).OrderBy(c => c.DueDate).ToList();
    }

    // ── Effective start ───────────────────────────────────────────────────────

    private static DateOnly ComputeEffectiveStart(Report report, DateOnly goLiveDate)
    {
        // SICRE operational start = first day of the month AFTER GoLiveDate
        var operationalStart = new DateOnly(goLiveDate.Year, goLiveDate.Month, 1).AddMonths(1);
        // Report legal start = first day of the report's StartDate month
        var reportStart = new DateOnly(report.StartDate.Year, report.StartDate.Month, 1);
        return Max(operationalStart, reportStart);
    }

    private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;

    // ── Auto-generable check ──────────────────────────────────────────────────

    private static bool IsAutoGenerable(Report report) =>
        report.GenerationMode == ReportGenerationMode.Automatic
        && report.Frequency != ReportFrequency.Eventual
        && report.DueDateRuleType != ReportDueDateRuleType.ManualDateRequired;

    // ── Rolling horizon ───────────────────────────────────────────────────────

    private static DateOnly GetRollingHorizon(ReportFrequency frequency, DateOnly today) =>
        frequency switch
        {
            ReportFrequency.Monthly or ReportFrequency.MonthlyAnticipated => today.AddMonths(2),
            ReportFrequency.Quarterly => today.AddMonths(6),
            ReportFrequency.SemiAnnual => today.AddMonths(12),
            ReportFrequency.Annual => today.AddMonths(24),
            _ => today.AddMonths(2),
        };

    // ── Candidate generation ──────────────────────────────────────────────────

    private IEnumerable<ReportInstanceCandidate> GenerateCandidates(
        Report report,
        DateOnly from,
        DateOnly to
    ) =>
        report.DueDateRuleType switch
        {
            ReportDueDateRuleType.DayOfMonth => GenerateMonthlyDayOfMonth(report, from, to),
            ReportDueDateRuleType.LastDayOfMonth => GenerateMonthlyLastDay(report, from, to),
            ReportDueDateRuleType.FixedDate => GenerateFixedDate(report, from, to),
            ReportDueDateRuleType.FixedDates => GenerateFixedDates(report, from, to),
            _ => [],
        };

    // ── DayOfMonth ────────────────────────────────────────────────────────────

    private static IEnumerable<ReportInstanceCandidate> GenerateMonthlyDayOfMonth(
        Report report,
        DateOnly from,
        DateOnly to
    )
    {
        if (!report.DueDateDay.HasValue)
            yield break;

        var cursor = new DateOnly(from.Year, from.Month, 1);
        while (cursor <= to)
        {
            var daysInMonth = DateTime.DaysInMonth(cursor.Year, cursor.Month);
            var day = Math.Min(report.DueDateDay.Value, daysInMonth);
            var dueDate = new DateOnly(cursor.Year, cursor.Month, day);

            if (dueDate >= from && dueDate <= to)
            {
                if (!report.EndDate.HasValue || dueDate <= report.EndDate.Value)
                    yield return BuildMonthlyCandidate(report, dueDate);
            }

            cursor = cursor.AddMonths(1);
        }
    }

    // ── LastDayOfMonth ────────────────────────────────────────────────────────

    private static IEnumerable<ReportInstanceCandidate> GenerateMonthlyLastDay(
        Report report,
        DateOnly from,
        DateOnly to
    )
    {
        var cursor = new DateOnly(from.Year, from.Month, 1);
        while (cursor <= to)
        {
            var lastDay = DateTime.DaysInMonth(cursor.Year, cursor.Month);
            var dueDate = new DateOnly(cursor.Year, cursor.Month, lastDay);

            if (dueDate >= from && dueDate <= to)
            {
                if (!report.EndDate.HasValue || dueDate <= report.EndDate.Value)
                    yield return BuildMonthlyCandidate(report, dueDate);
            }

            cursor = cursor.AddMonths(1);
        }
    }

    private static ReportInstanceCandidate BuildMonthlyCandidate(Report report, DateOnly dueDate)
    {
        var periodStart = new DateOnly(dueDate.Year, dueDate.Month, 1);
        var periodEnd = new DateOnly(
            dueDate.Year,
            dueDate.Month,
            DateTime.DaysInMonth(dueDate.Year, dueDate.Month)
        );
        var periodName = $"{SpanishMonths[dueDate.Month]} {dueDate.Year}";
        return new ReportInstanceCandidate(
            report.Id,
            dueDate.Year,
            dueDate.Month,
            periodName,
            periodStart,
            periodEnd,
            dueDate,
            null
        );
    }

    // ── FixedDate (annual single date) ────────────────────────────────────────

    private static IEnumerable<ReportInstanceCandidate> GenerateFixedDate(
        Report report,
        DateOnly from,
        DateOnly to
    )
    {
        if (!report.DueDateDay.HasValue || !report.DueDateMonth.HasValue)
            yield break;

        for (var year = from.Year; year <= to.Year + 1; year++)
        {
            var daysInMonth = DateTime.DaysInMonth(year, report.DueDateMonth.Value);
            var day = Math.Min(report.DueDateDay.Value, daysInMonth);
            var dueDate = new DateOnly(year, report.DueDateMonth.Value, day);

            if (dueDate < from)
                continue;
            if (dueDate > to)
                yield break;
            if (report.EndDate.HasValue && dueDate > report.EndDate.Value)
                yield break;

            var periodStart = new DateOnly(dueDate.Year, 1, 1);
            var periodEnd = new DateOnly(dueDate.Year, 12, 31);
            var periodName = $"Anual {dueDate.Year}";

            yield return new ReportInstanceCandidate(
                report.Id,
                dueDate.Year,
                1,
                periodName,
                periodStart,
                periodEnd,
                dueDate,
                null
            );
        }
    }

    // ── FixedDates (multiple fixed dates per year, e.g. IFE1) ─────────────────

    private IEnumerable<ReportInstanceCandidate> GenerateFixedDates(
        Report report,
        DateOnly from,
        DateOnly to
    )
    {
        var items = ParseDatesDefinition(report);
        if (items is null)
            yield break;

        var ordered = items.OrderBy(x => x.Month).ThenBy(x => x.Day).ToList();

        for (var year = from.Year; year <= to.Year + 1; year++)
        {
            foreach (var item in ordered)
            {
                var daysInMonth = DateTime.DaysInMonth(year, item.Month);
                if (item.Day > daysInMonth)
                    continue;

                var dueDate = new DateOnly(year, item.Month, item.Day);

                if (dueDate < from)
                    continue;
                if (dueDate > to)
                    yield break;
                if (report.EndDate.HasValue && dueDate > report.EndDate.Value)
                    yield break;

                var periodStart = new DateOnly(dueDate.Year, dueDate.Month, 1);
                var periodEnd = new DateOnly(
                    dueDate.Year,
                    dueDate.Month,
                    DateTime.DaysInMonth(dueDate.Year, dueDate.Month)
                );
                var periodName = $"{SpanishMonths[dueDate.Month]} {dueDate.Year}";

                yield return new ReportInstanceCandidate(
                    report.Id,
                    dueDate.Year,
                    dueDate.Month,
                    periodName,
                    periodStart,
                    periodEnd,
                    dueDate,
                    null
                );
            }
        }
    }

    // ── JSON parsing ──────────────────────────────────────────────────────────

    private List<FixedDateItem>? ParseDatesDefinition(Report report)
    {
        if (string.IsNullOrWhiteSpace(report.DueDateDatesDefinition))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<FixedDateItem>>(
                report.DueDateDatesDefinition,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Report {ReportId}: invalid DueDateDatesDefinition JSON",
                report.Id
            );
            return null;
        }
    }

    private sealed record FixedDateItem
    {
        public int Month { get; init; }
        public int Day { get; init; }
    }
}
