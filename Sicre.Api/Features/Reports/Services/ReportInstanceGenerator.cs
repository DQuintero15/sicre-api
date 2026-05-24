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
    /// Returns the next period candidate for the Hangfire rolling-horizon job.
    /// Returns null if the report is not auto-generable or there is nothing to generate yet.
    /// </summary>
    ReportInstanceCandidate? GetNextCandidate(
        Report report,
        ReportInstance? latestInstance,
        DateOnly goLiveDate
    );

    /// <summary>
    /// Returns all candidates that fall inside the given window (used for 12-month projection).
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
        if (!IsAutogenerableReport(report))
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = GetRollingHorizon(report.Frequency, today);

        DateOnly cursor = latestInstance is null ? today : latestInstance.PeriodEnd.AddDays(1);

        if (report.DueDateRuleType is ReportDueDateRuleType.FixedDateSet)
            return GetNextFixedDateCandidate(report, latestInstance, cursor, horizon, goLiveDate);

        if (report.DueDateRuleType is ReportDueDateRuleType.DateRangeSet)
            return GetNextDateRangeCandidate(report, latestInstance, cursor, horizon, goLiveDate);

        // Standard periodic — loop until we find a valid candidate or exceed the horizon
        var currentCursor = cursor;
        while (true)
        {
            var (periodStart, periodEnd) = ComputePeriodBoundsFromCursor(
                report.Frequency,
                currentCursor
            );
            var dueDate = ComputeDueDate(report, periodStart, periodEnd);
            if (dueDate is null)
                return null;
            if (dueDate.Value > horizon)
                return null;

            // Skip periods whose end precedes the report's start date
            if (periodEnd < report.StartDate)
            {
                currentCursor = AdvanceCursorByFrequency(report.Frequency, periodEnd);
                continue;
            }

            // Stop if the report has expired
            if (report.EndDate.HasValue && periodStart > report.EndDate.Value)
                return null;

            // Skip if DueDate falls before SICRE's operational start
            if (dueDate.Value < goLiveDate)
            {
                currentCursor = AdvanceCursorByFrequency(report.Frequency, periodEnd);
                continue;
            }

            var (periodYear, periodMonth) = GetPeriodYearMonth(report.Frequency, periodStart);
            var periodName = BuildPeriodName(report.Frequency, periodStart, periodMonth);

            return new ReportInstanceCandidate(
                report.Id,
                periodYear,
                periodMonth,
                periodName,
                periodStart,
                periodEnd,
                dueDate.Value,
                null
            );
        }
    }

    public IReadOnlyList<ReportInstanceCandidate> GetCandidatesInWindow(
        Report report,
        DateOnly windowStart,
        DateOnly windowEnd,
        DateOnly goLiveDate
    )
    {
        var results = new List<ReportInstanceCandidate>();

        if (report.Frequency is ReportFrequency.Eventual)
            return results;

        if (report.DueDateRuleType is ReportDueDateRuleType.FixedDateSet)
        {
            results.AddRange(
                GetFixedDateCandidatesInWindow(report, windowStart, windowEnd, goLiveDate)
            );
            return results;
        }

        if (report.DueDateRuleType is ReportDueDateRuleType.DateRangeSet)
        {
            results.AddRange(
                GetDateRangeCandidatesInWindow(report, windowStart, windowEnd, goLiveDate)
            );
            return results;
        }

        if (
            report.DueDateRuleType
            is ReportDueDateRuleType.DaysAfterEvent
                or ReportDueDateRuleType.ManualDateRequired
        )
            return results;

        var cursor = windowStart;
        while (cursor <= windowEnd)
        {
            var (periodStart, periodEnd) = ComputePeriodBoundsFromCursor(report.Frequency, cursor);

            // Stop if the report has expired
            if (report.EndDate.HasValue && periodStart > report.EndDate.Value)
                break;

            var dueDate = ComputeDueDate(report, periodStart, periodEnd);
            if (
                dueDate is not null
                && dueDate.Value >= windowStart
                && dueDate.Value <= windowEnd
                && dueDate.Value >= goLiveDate
                && periodEnd >= report.StartDate
            )
            {
                var (periodYear, periodMonth) = GetPeriodYearMonth(report.Frequency, periodStart);
                var periodName = BuildPeriodName(report.Frequency, periodStart, periodMonth);
                results.Add(
                    new ReportInstanceCandidate(
                        report.Id,
                        periodYear,
                        periodMonth,
                        periodName,
                        periodStart,
                        periodEnd,
                        dueDate.Value,
                        null
                    )
                );
            }

            cursor = AdvanceCursorByFrequency(report.Frequency, periodEnd);
        }

        return results;
    }

    // ── Auto-generable check ──────────────────────────────────────────────────

    private static bool IsAutogenerableReport(Report report) =>
        report.GenerationMode == ReportGenerationMode.Automatic
        && report.Frequency != ReportFrequency.Eventual
        && report.DueDateRuleType != ReportDueDateRuleType.DaysAfterEvent
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

    // ── Period bounds computation ─────────────────────────────────────────────

    private static (DateOnly start, DateOnly end) ComputePeriodBoundsFromCursor(
        ReportFrequency frequency,
        DateOnly cursor
    ) =>
        frequency switch
        {
            ReportFrequency.Monthly or ReportFrequency.MonthlyAnticipated => (
                new DateOnly(cursor.Year, cursor.Month, 1),
                new DateOnly(
                    cursor.Year,
                    cursor.Month,
                    DateTime.DaysInMonth(cursor.Year, cursor.Month)
                )
            ),

            ReportFrequency.Quarterly => ComputeQuarterBounds(cursor),

            ReportFrequency.SemiAnnual => ComputeSemesterBounds(cursor),

            ReportFrequency.Annual => (
                new DateOnly(cursor.Year, 1, 1),
                new DateOnly(cursor.Year, 12, 31)
            ),

            _ => (cursor, cursor),
        };

    private static (DateOnly start, DateOnly end) ComputeQuarterBounds(DateOnly cursor)
    {
        var q = (cursor.Month - 1) / 3;
        var startMonth = q * 3 + 1;
        var endMonth = startMonth + 2;
        return (
            new DateOnly(cursor.Year, startMonth, 1),
            new DateOnly(cursor.Year, endMonth, DateTime.DaysInMonth(cursor.Year, endMonth))
        );
    }

    private static (DateOnly start, DateOnly end) ComputeSemesterBounds(DateOnly cursor)
    {
        var s = cursor.Month <= 6 ? 0 : 1;
        var startMonth = s * 6 + 1;
        var endMonth = startMonth + 5;
        return (
            new DateOnly(cursor.Year, startMonth, 1),
            new DateOnly(cursor.Year, endMonth, DateTime.DaysInMonth(cursor.Year, endMonth))
        );
    }

    private static DateOnly AdvanceCursorByFrequency(ReportFrequency frequency, DateOnly periodEnd) =>
        periodEnd.AddDays(1);

    // ── PeriodYear / PeriodMonth ──────────────────────────────────────────────

    private static (int year, int? month) GetPeriodYearMonth(
        ReportFrequency frequency,
        DateOnly periodStart
    ) =>
        frequency switch
        {
            ReportFrequency.Monthly or ReportFrequency.MonthlyAnticipated => (
                periodStart.Year,
                periodStart.Month
            ),
            ReportFrequency.Quarterly => (periodStart.Year, periodStart.Month),
            ReportFrequency.SemiAnnual => (periodStart.Year, periodStart.Month),
            ReportFrequency.Annual => (periodStart.Year, 1),
            _ => (periodStart.Year, null),
        };

    // ── PeriodName ────────────────────────────────────────────────────────────

    private static string BuildPeriodName(
        ReportFrequency frequency,
        DateOnly periodStart,
        int? periodMonth
    ) =>
        frequency switch
        {
            ReportFrequency.Monthly
            or ReportFrequency.MonthlyAnticipated =>
                $"{SpanishMonths[periodStart.Month]} {periodStart.Year}",

            ReportFrequency.Quarterly =>
                $"T{((periodStart.Month - 1) / 3) + 1} {periodStart.Year}",

            ReportFrequency.SemiAnnual =>
                $"S{(periodStart.Month <= 6 ? 1 : 2)} {periodStart.Year}",

            ReportFrequency.Annual => $"Anual {periodStart.Year}",

            _ => $"Periodo {periodStart.Year}",
        };

    // ── DueDate computation ───────────────────────────────────────────────────

    private DateOnly? ComputeDueDate(Report report, DateOnly periodStart, DateOnly periodEnd) =>
        report.DueDateRuleType switch
        {
            ReportDueDateRuleType.DayNumberOfPeriod => ComputeDayNumberOfPeriod(
                report,
                periodStart
            ),

            ReportDueDateRuleType.LastDayOfPeriod => ComputeLastDayOfPeriod(report, periodStart),

            ReportDueDateRuleType.DaysAfterPeriodEnd => report.DueDateDaysToAdd.HasValue
                ? periodEnd.AddDays(report.DueDateDaysToAdd.Value)
                : null,

            ReportDueDateRuleType.DaysAfterEvent => null,

            ReportDueDateRuleType.FixedDateSet => null,

            ReportDueDateRuleType.DateRangeSet => null,

            ReportDueDateRuleType.SpecificDate => report.DueDateSpecificDate,

            ReportDueDateRuleType.ManualDateRequired => null,

            _ => null,
        };

    private static DateOnly? ComputeDayNumberOfPeriod(Report report, DateOnly periodStart)
    {
        if (!report.DueDateDayNumber.HasValue)
            return null;

        var monthOffset = report.DueDateMonthOffset ?? 0;
        var targetTotalMonth = periodStart.Month + monthOffset;
        var targetYear = periodStart.Year + (targetTotalMonth - 1) / 12;
        var targetMonth = ((targetTotalMonth - 1) % 12) + 1;

        var daysInTargetMonth = DateTime.DaysInMonth(targetYear, targetMonth);
        var day = Math.Min(report.DueDateDayNumber.Value, daysInTargetMonth);

        return new DateOnly(targetYear, targetMonth, day);
    }

    private static DateOnly? ComputeLastDayOfPeriod(Report report, DateOnly periodStart)
    {
        var monthOffset = report.DueDateMonthOffset ?? 0;
        var targetTotalMonth = periodStart.Month + monthOffset;
        var targetYear = periodStart.Year + (targetTotalMonth - 1) / 12;
        var targetMonth = ((targetTotalMonth - 1) % 12) + 1;

        var lastDay = DateTime.DaysInMonth(targetYear, targetMonth);
        return new DateOnly(targetYear, targetMonth, lastDay);
    }

    // ── FixedDateSet logic ────────────────────────────────────────────────────

    private ReportInstanceCandidate? GetNextFixedDateCandidate(
        Report report,
        ReportInstance? latestInstance,
        DateOnly cursor,
        DateOnly horizon,
        DateOnly goLiveDate
    )
    {
        var items = ParseFixedDates(report);
        if (items is null)
            return null;

        for (var year = cursor.Year; year <= horizon.Year + 1; year++)
        {
            foreach (var item in items.OrderBy(x => x.Month).ThenBy(x => x.Day))
            {
                var (candidate, _) = BuildFixedDateCandidate(report, item, year);
                if (candidate is null)
                    continue;

                if (candidate.DueDate < cursor)
                    continue;
                if (candidate.DueDate > horizon)
                    return null;

                if (candidate.PeriodEnd < report.StartDate)
                    continue;
                if (report.EndDate.HasValue && candidate.PeriodStart > report.EndDate.Value)
                    continue;
                if (candidate.DueDate < goLiveDate)
                    continue;

                if (
                    latestInstance is not null
                    && latestInstance.PeriodYear == candidate.PeriodYear
                    && latestInstance.PeriodMonth == candidate.PeriodMonth
                )
                    continue;

                return candidate;
            }
        }

        return null;
    }

    private IReadOnlyList<ReportInstanceCandidate> GetFixedDateCandidatesInWindow(
        Report report,
        DateOnly windowStart,
        DateOnly windowEnd,
        DateOnly goLiveDate
    )
    {
        var items = ParseFixedDates(report);
        if (items is null)
            return [];

        var results = new List<ReportInstanceCandidate>();

        for (var year = windowStart.Year; year <= windowEnd.Year + 1; year++)
        {
            foreach (var item in items.OrderBy(x => x.Month).ThenBy(x => x.Day))
            {
                var (candidate, _) = BuildFixedDateCandidate(report, item, year);
                if (candidate is null)
                    continue;
                if (
                    candidate.DueDate >= windowStart
                    && candidate.DueDate <= windowEnd
                    && candidate.DueDate >= goLiveDate
                    && candidate.PeriodEnd >= report.StartDate
                    && (!report.EndDate.HasValue || candidate.PeriodStart <= report.EndDate.Value)
                )
                    results.Add(candidate);
            }
        }

        return results;
    }

    private (ReportInstanceCandidate? candidate, int dueDateYear) BuildFixedDateCandidate(
        Report report,
        FixedDateItem item,
        int year
    )
    {
        var daysInMonth = DateTime.DaysInMonth(year, item.Month);
        if (item.Day > daysInMonth)
            return (null, year);

        var dueDate = new DateOnly(year, item.Month, item.Day);
        int periodYear = year;
        int? periodMonth;
        string periodName;

        if (item.ReportedQuarter.HasValue)
        {
            var periodYearOffset = item.PeriodYearOffset ?? 0;
            periodYear = year + periodYearOffset;
            var quarterFirstMonth = (item.ReportedQuarter.Value - 1) * 3 + 1;
            periodMonth = quarterFirstMonth;
            periodName = $"T{item.ReportedQuarter.Value} {periodYear}";
        }
        else if (item.ReportedSemester.HasValue)
        {
            var periodYearOffset = item.PeriodYearOffset ?? 0;
            periodYear = year + periodYearOffset;
            var semesterFirstMonth = (item.ReportedSemester.Value - 1) * 6 + 1;
            periodMonth = semesterFirstMonth;
            periodName = $"S{item.ReportedSemester.Value} {periodYear}";
        }
        else
        {
            periodMonth = item.Month;
            periodName = $"Periodo {item.Month}/{year}";
        }

        var periodDate = new DateOnly(year, item.Month, item.Day);
        return (
            new ReportInstanceCandidate(
                report.Id,
                periodYear,
                periodMonth,
                periodName,
                periodDate,
                periodDate,
                dueDate,
                null
            ),
            year
        );
    }

    private List<FixedDateItem>? ParseFixedDates(Report report)
    {
        if (!string.IsNullOrWhiteSpace(report.DueDateFixedDatesDefinition))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<FixedDateItem>>(
                    report.DueDateFixedDatesDefinition,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                return items;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Report {ReportId}: invalid DueDateFixedDatesDefinition JSON: {Json}",
                    report.Id,
                    report.DueDateFixedDatesDefinition
                );
                return null;
            }
        }

        if (report.DueDateFixedMonth.HasValue && report.DueDateFixedDay.HasValue)
        {
            return
            [
                new FixedDateItem
                {
                    Month = report.DueDateFixedMonth.Value,
                    Day = report.DueDateFixedDay.Value,
                },
            ];
        }

        return null;
    }

    // ── DateRangeSet logic ────────────────────────────────────────────────────

    private ReportInstanceCandidate? GetNextDateRangeCandidate(
        Report report,
        ReportInstance? latestInstance,
        DateOnly cursor,
        DateOnly horizon,
        DateOnly goLiveDate
    )
    {
        var ranges = ParseDateRanges(report);
        if (ranges is null)
            return null;

        for (var year = cursor.Year; year <= horizon.Year + 1; year++)
        {
            foreach (var range in ranges.OrderBy(x => x.EndMonth).ThenBy(x => x.EndDay))
            {
                var candidate = BuildDateRangeCandidate(report, range, year);
                if (candidate is null)
                    continue;

                if (candidate.DueDate < cursor)
                    continue;
                if (candidate.DueDate > horizon)
                    return null;

                if (candidate.PeriodEnd < report.StartDate)
                    continue;
                if (report.EndDate.HasValue && candidate.PeriodStart > report.EndDate.Value)
                    continue;
                if (candidate.DueDate < goLiveDate)
                    continue;

                if (
                    latestInstance is not null
                    && latestInstance.PeriodYear == candidate.PeriodYear
                    && latestInstance.PeriodMonth == candidate.PeriodMonth
                )
                    continue;

                return candidate;
            }
        }

        return null;
    }

    private IReadOnlyList<ReportInstanceCandidate> GetDateRangeCandidatesInWindow(
        Report report,
        DateOnly windowStart,
        DateOnly windowEnd,
        DateOnly goLiveDate
    )
    {
        var ranges = ParseDateRanges(report);
        if (ranges is null)
            return [];

        var results = new List<ReportInstanceCandidate>();

        for (var year = windowStart.Year; year <= windowEnd.Year + 1; year++)
        {
            foreach (var range in ranges.OrderBy(x => x.EndMonth).ThenBy(x => x.EndDay))
            {
                var candidate = BuildDateRangeCandidate(report, range, year);
                if (candidate is null)
                    continue;
                if (
                    candidate.DueDate >= windowStart
                    && candidate.DueDate <= windowEnd
                    && candidate.DueDate >= goLiveDate
                    && candidate.PeriodEnd >= report.StartDate
                    && (!report.EndDate.HasValue || candidate.PeriodStart <= report.EndDate.Value)
                )
                    results.Add(candidate);
            }
        }

        return results;
    }

    private static ReportInstanceCandidate? BuildDateRangeCandidate(
        Report report,
        DateRangeItem range,
        int year
    )
    {
        var endDaysInMonth = DateTime.DaysInMonth(year, range.EndMonth);
        if (range.EndDay > endDaysInMonth)
            return null;

        var startDaysInMonth = DateTime.DaysInMonth(year, range.StartMonth);
        if (range.StartDay > startDaysInMonth)
            return null;

        var periodStart = new DateOnly(year, range.StartMonth, range.StartDay);
        var dueDate = new DateOnly(year, range.EndMonth, range.EndDay);
        var periodName = $"Período {range.StartMonth}-{range.EndMonth}/{year}";

        return new ReportInstanceCandidate(
            report.Id,
            year,
            range.StartMonth,
            periodName,
            periodStart,
            dueDate,
            dueDate,
            null
        );
    }

    private List<DateRangeItem>? ParseDateRanges(Report report)
    {
        if (string.IsNullOrWhiteSpace(report.DueDateRangesDefinition))
            return null;

        try
        {
            var ranges = JsonSerializer.Deserialize<List<DateRangeItem>>(
                report.DueDateRangesDefinition,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            return ranges;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Report {ReportId}: invalid DueDateRangesDefinition JSON: {Json}",
                report.Id,
                report.DueDateRangesDefinition
            );
            return null;
        }
    }

    // ── JSON helper record types ──────────────────────────────────────────────

    private sealed record FixedDateItem
    {
        public int Month { get; init; }
        public int Day { get; init; }
        public int? ReportedQuarter { get; init; }
        public int? ReportedSemester { get; init; }
        public int? PeriodYearOffset { get; init; }
    }

    private sealed record DateRangeItem
    {
        public int StartMonth { get; init; }
        public int StartDay { get; init; }
        public int EndMonth { get; init; }
        public int EndDay { get; init; }
    }
}
