using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.Analytics.DTOs;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Analytics.Services;

public class AnalyticsService(
    ApplicationDbContext context,
    IDateHelper dateHelper,
    ILogger<AnalyticsService> logger
) : IAnalyticsService
{
    private static readonly string[] ElevatedRoles = ["Administrator", "Auditor"];

    private IQueryable<Domain.Entities.ReportInstance> BuildBaseQuery(
        Guid userId,
        IList<string> userRoles
    )
    {
        var query = context.ReportInstances.AsNoTracking().AsQueryable();

        if (!userRoles.Intersect(ElevatedRoles, StringComparer.OrdinalIgnoreCase).Any())
        {
            query = query.Where(i =>
                i.ResponsibleUserId == userId
                || i.SupervisorUserId == userId
                || (
                    i.Report != null
                    && (
                        i.Report.SenderResponsibleUserId == userId
                        || i.Report.EntityUploadResponsibleUserId == userId
                        || i.Report.FollowUpLeaderUserId == userId
                    )
                )
            );
        }

        return query;
    }

    private static IQueryable<Domain.Entities.ReportInstance> ApplyFilters(
        IQueryable<Domain.Entities.ReportInstance> query,
        AnalyticsFilterRequest filter
    )
    {
        if (filter.StartDate.HasValue)
            query = query.Where(i => i.DueDate >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(i => i.DueDate <= filter.EndDate.Value);

        if (filter.ControlEntityId.HasValue)
            query = query.Where(i =>
                i.Report != null && i.Report.ControlEntityId == filter.ControlEntityId.Value
            );

        if (filter.ResponsibleUserId.HasValue)
            query = query.Where(i => i.ResponsibleUserId == filter.ResponsibleUserId.Value);

        if (filter.BranchId.HasValue)
            query = query.Where(i =>
                i.Report != null && i.Report.BranchId == filter.BranchId.Value
            );

        return query;
    }

    private static bool IsUpcomingDue(DateOnly dueDate, int alertCriticalDays, DateOnly today)
    {
        var days = (
            dueDate.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)
        ).Days;
        return days >= 0 && days <= alertCriticalDays;
    }

    public async Task<ApiResponse<StateDistributionDto>> GetStateDistributionAsync(
        Guid userId,
        IList<string> userRoles,
        AnalyticsFilterRequest filter
    )
    {
        try
        {
            var query = BuildBaseQuery(userId, userRoles);
            query = ApplyFilters(query, filter);

            var today = dateHelper.GetCurrentDate();

            var flat = await query
                .Select(i => new
                {
                    i.Status,
                    i.DueDate,
                    AlertCriticalDays = i.Report != null ? i.Report.AlertCriticalDays : 30,
                })
                .ToListAsync();

            var onTime = flat.Count(i => i.Status == ReportStatus.SentOnTime);
            var late = flat.Count(i => i.Status == ReportStatus.SentLate);
            var overdue = flat.Count(i => i.Status == ReportStatus.Overdue);
            var upcomingDue = flat.Count(i =>
                i.Status == ReportStatus.Pending
                && IsUpcomingDue(i.DueDate, i.AlertCriticalDays, today)
            );
            var pending = flat.Count(i =>
                i.Status == ReportStatus.Pending
                && !IsUpcomingDue(i.DueDate, i.AlertCriticalDays, today)
            );

            return ApiResponse<StateDistributionDto>.Ok(
                new StateDistributionDto
                {
                    Total = flat.Count,
                    OnTime = onTime,
                    Late = late,
                    Overdue = overdue,
                    UpcomingDue = upcomingDue,
                    Pending = pending,
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting state distribution");
            return ApiResponse<StateDistributionDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error obteniendo analíticas"
            );
        }
    }

    public async Task<ApiResponse<List<ComplianceTrendDto>>> GetComplianceTrendAsync(
        Guid userId,
        IList<string> userRoles,
        AnalyticsFilterRequest filter
    )
    {
        try
        {
            var query = BuildBaseQuery(userId, userRoles);
            query = ApplyFilters(query, filter);

            if (!filter.StartDate.HasValue && !filter.EndDate.HasValue)
            {
                var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-11));
                cutoff = new DateOnly(cutoff.Year, cutoff.Month, 1);
                query = query.Where(i => i.DueDate >= cutoff);
            }

            var flatData = await query.Select(i => new { i.DueDate, i.Status }).ToListAsync();

            var grouped = flatData
                .GroupBy(x => new { x.DueDate.Year, x.DueDate.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g =>
                {
                    var total = g.Count();
                    var onTime = g.Count(x => x.Status == ReportStatus.SentOnTime);
                    var late = g.Count(x => x.Status == ReportStatus.SentLate);
                    var overdue = g.Count(x => x.Status == ReportStatus.Overdue);
                    var pend = g.Count(x => x.Status == ReportStatus.Pending);

                    return new ComplianceTrendDto
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Total = total,
                        OnTime = onTime,
                        Late = late,
                        Overdue = overdue,
                        Pending = pend,
                        OnTimePercentage =
                            total > 0 ? Math.Round((double)onTime / total * 100, 2) : 0,
                        LatePercentage = total > 0 ? Math.Round((double)late / total * 100, 2) : 0,
                        OverduePercentage =
                            total > 0 ? Math.Round((double)overdue / total * 100, 2) : 0,
                        PendingPercentage =
                            total > 0 ? Math.Round((double)pend / total * 100, 2) : 0,
                    };
                })
                .ToList();

            return ApiResponse<List<ComplianceTrendDto>>.Ok(grouped);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting compliance trend");
            return ApiResponse<List<ComplianceTrendDto>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error obteniendo tendencia de cumplimiento"
            );
        }
    }

    public async Task<ApiResponse<List<EntityComplianceDto>>> GetComplianceByEntityAsync(
        Guid userId,
        IList<string> userRoles,
        AnalyticsFilterRequest filter
    )
    {
        try
        {
            var query = BuildBaseQuery(userId, userRoles);
            query = query.Include(i => i.Report).ThenInclude(r => r!.ControlEntity);
            query = ApplyFilters(query, filter);

            var flatData = await query
                .Select(i => new
                {
                    EntityName = i.Report != null && i.Report.ControlEntity != null
                        ? i.Report.ControlEntity.Name
                        : "Desconocido",
                    i.Status,
                })
                .ToListAsync();

            var grouped = flatData
                .GroupBy(x => x.EntityName)
                .Select(g =>
                {
                    var total = g.Count();
                    var onTime = g.Count(x => x.Status == ReportStatus.SentOnTime);
                    var late = g.Count(x => x.Status == ReportStatus.SentLate);
                    var overdue = g.Count(x => x.Status == ReportStatus.Overdue);
                    var pend = g.Count(x => x.Status == ReportStatus.Pending);

                    return new EntityComplianceDto
                    {
                        EntityName = g.Key,
                        Total = total,
                        OnTime = onTime,
                        Late = late,
                        Overdue = overdue,
                        Pending = pend,
                        OnTimeRate = total > 0 ? Math.Round((double)onTime / total * 100, 2) : 0,
                        DeliveryRate =
                            total > 0 ? Math.Round((double)(onTime + late) / total * 100, 2) : 0,
                    };
                })
                .OrderByDescending(x => x.OnTimeRate)
                .ToList();

            return ApiResponse<List<EntityComplianceDto>>.Ok(grouped);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting compliance by entity");
            return ApiResponse<List<EntityComplianceDto>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error obteniendo cumplimiento por entidad"
            );
        }
    }

    public async Task<ApiResponse<List<ResponsibleComplianceDto>>> GetComplianceByResponsibleAsync(
        Guid userId,
        IList<string> userRoles,
        AnalyticsFilterRequest filter
    )
    {
        try
        {
            var query = BuildBaseQuery(userId, userRoles);
            query = ApplyFilters(query, filter);

            var flatData = await query
                .Select(i => new
                {
                    ResponsibleName = i.ResponsibleUser != null
                        ? i.ResponsibleUser.FirstName + " " + i.ResponsibleUser.LastName
                        : "Sin Asignar",
                    i.Status,
                })
                .ToListAsync();

            var grouped = flatData
                .GroupBy(x => x.ResponsibleName)
                .Select(g =>
                {
                    var total = g.Count();
                    var onTime = g.Count(x => x.Status == ReportStatus.SentOnTime);
                    var late = g.Count(x => x.Status == ReportStatus.SentLate);
                    var overdue = g.Count(x => x.Status == ReportStatus.Overdue);
                    var pend = g.Count(x => x.Status == ReportStatus.Pending);

                    return new ResponsibleComplianceDto
                    {
                        ResponsibleName = g.Key,
                        Total = total,
                        OnTime = onTime,
                        Late = late,
                        Overdue = overdue,
                        Pending = pend,
                        OnTimeRate = total > 0 ? Math.Round((double)onTime / total * 100, 2) : 0,
                        DeliveryRate =
                            total > 0 ? Math.Round((double)(onTime + late) / total * 100, 2) : 0,
                    };
                })
                .OrderByDescending(x => x.Overdue)
                .ToList();

            return ApiResponse<List<ResponsibleComplianceDto>>.Ok(grouped);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting compliance by responsible");
            return ApiResponse<List<ResponsibleComplianceDto>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error obteniendo cumplimiento por responsable"
            );
        }
    }

    public async Task<ApiResponse<MonthlyInstancesMetricsDto>> GetMonthlyInstancesMetricsAsync(
        Guid userId,
        IList<string> userRoles,
        int month,
        int year
    )
    {
        try
        {
            var query = BuildBaseQuery(userId, userRoles);

            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            query = query.Where(i => i.DueDate >= startDate && i.DueDate <= endDate);

            var today = dateHelper.GetCurrentDate();

            var instances = await query
                .Select(i => new
                {
                    i.Id,
                    ReportCode = i.Report != null ? i.Report.Code : string.Empty,
                    ReportName = i.Report != null ? i.Report.Name : string.Empty,
                    i.DueDate,
                    i.Status,
                    i.PeriodName,
                    AlertCriticalDays = i.Report != null ? i.Report.AlertCriticalDays : 30,
                })
                .ToListAsync();

            var onTime = instances.Count(i => i.Status == ReportStatus.SentOnTime);
            var late = instances.Count(i => i.Status == ReportStatus.SentLate);
            var overdue = instances.Count(i => i.Status == ReportStatus.Overdue);
            var upcomingDue = instances.Count(i =>
                i.Status == ReportStatus.Pending
                && IsUpcomingDue(i.DueDate, i.AlertCriticalDays, today)
            );
            var pending = instances.Count(i =>
                i.Status == ReportStatus.Pending
                && !IsUpcomingDue(i.DueDate, i.AlertCriticalDays, today)
            );

            var total = instances.Count;
            var delivered = onTime + late;
            var compliancePct = total > 0 ? Math.Round((double)delivered / total * 100, 2) : 0;
            var onTimePct = total > 0 ? Math.Round((double)onTime / total * 100, 2) : 0;

            var dayGroups = instances
                .GroupBy(i => i.DueDate.Day)
                .ToDictionary(
                    g => g.Key,
                    g =>
                        g.Select(i =>
                            {
                                var statusStr =
                                    i.Status == ReportStatus.Pending
                                    && IsUpcomingDue(i.DueDate, i.AlertCriticalDays, today)
                                        ? "UpcomingDue"
                                        : i.Status.ToString();

                                return new CalendarInstanceDto
                                {
                                    Id = i.Id,
                                    ReportCode = i.ReportCode,
                                    ReportName = i.ReportName,
                                    DueDate = i.DueDate.ToString("yyyy-MM-dd"),
                                    Status = statusStr,
                                    PeriodName = i.PeriodName,
                                    Datetime = i
                                        .DueDate.ToDateTime(TimeOnly.MinValue)
                                        .ToString("yyyy-MM-ddTHH:mm:ss"),
                                };
                            })
                            .ToList()
                );

            return ApiResponse<MonthlyInstancesMetricsDto>.Ok(
                new MonthlyInstancesMetricsDto
                {
                    Metrics = new MonthMetricsDto
                    {
                        TotalInstances = total,
                        OnTimeCount = onTime,
                        LateCount = late,
                        OverdueCount = overdue,
                        UpcomingDueCount = upcomingDue,
                        PendingCount = pending,
                        CompliancePercentage = compliancePct,
                        OnTimePercentage = onTimePct,
                    },
                    DayGroups = dayGroups,
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting monthly instances metrics");
            return ApiResponse<MonthlyInstancesMetricsDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error obteniendo métricas mensuales"
            );
        }
    }

    public async Task<ApiResponse<List<BranchComplianceDto>>> GetComplianceByBranchAsync(
        Guid userId,
        IList<string> userRoles,
        AnalyticsFilterRequest filter
    )
    {
        try
        {
            var query = BuildBaseQuery(userId, userRoles);
            query = query.Include(i => i.Report).ThenInclude(r => r!.Branch);
            query = ApplyFilters(query, filter);

            var flatData = await query
                .Select(i => new
                {
                    BranchName = i.Report != null && i.Report.Branch != null
                        ? i.Report.Branch.Name
                        : "Sin Sede",
                    i.Status,
                })
                .ToListAsync();

            var grouped = flatData
                .GroupBy(x => x.BranchName)
                .Select(g =>
                {
                    var total = g.Count();
                    var onTime = g.Count(x => x.Status == ReportStatus.SentOnTime);
                    var late = g.Count(x => x.Status == ReportStatus.SentLate);
                    var overdue = g.Count(x => x.Status == ReportStatus.Overdue);
                    var pend = g.Count(x => x.Status == ReportStatus.Pending);

                    return new BranchComplianceDto(
                        BranchName: g.Key,
                        Total: total,
                        OnTime: onTime,
                        Late: late,
                        Overdue: overdue,
                        Pending: pend,
                        OnTimeRate: total > 0 ? Math.Round((double)onTime / total * 100, 2) : 0,
                        DeliveryRate: total > 0
                            ? Math.Round((double)(onTime + late) / total * 100, 2)
                            : 0
                    );
                })
                .OrderByDescending(x => x.OnTimeRate)
                .ToList();

            return ApiResponse<List<BranchComplianceDto>>.Ok(grouped);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting compliance by branch");
            return ApiResponse<List<BranchComplianceDto>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error obteniendo cumplimiento por sede"
            );
        }
    }

    public async Task<ApiResponse<List<ReversionSummaryDto>>> GetReversionsByPeriodAsync(
        Guid userId,
        IList<string> userRoles,
        AnalyticsFilterRequest filter
    )
    {
        try
        {
            var isElevated = userRoles
                .Intersect(ElevatedRoles, StringComparer.OrdinalIgnoreCase)
                .Any();

            var query = context
                .ReportReversions.AsNoTracking()
                .Include(r => r.ReportInstance)
                    .ThenInclude(i => i!.Report)
                .Include(r => r.CreatedByUser)
                .AsQueryable();

            if (!isElevated)
                query = query.Where(r =>
                    r.ReportInstance != null
                    && (
                        r.ReportInstance.ResponsibleUserId == userId
                        || r.ReportInstance.SupervisorUserId == userId
                        || (
                            r.ReportInstance.Report != null
                            && (
                                r.ReportInstance.Report.SenderResponsibleUserId == userId
                                || r.ReportInstance.Report.EntityUploadResponsibleUserId == userId
                                || r.ReportInstance.Report.FollowUpLeaderUserId == userId
                            )
                        )
                    )
                );

            if (filter.StartDate.HasValue)
                query = query.Where(r =>
                    r.CreatedAt >= filter.StartDate.Value.ToDateTime(TimeOnly.MinValue)
                );

            if (filter.EndDate.HasValue)
                query = query.Where(r =>
                    r.CreatedAt < filter.EndDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue)
                );

            var result = await query
                .OrderBy(r => r.CreatedAt)
                .Select(r => new ReversionSummaryDto
                {
                    ReportName =
                        r.ReportInstance != null && r.ReportInstance.Report != null
                            ? r.ReportInstance.Report.Name
                            : "—",
                    ReportCode =
                        r.ReportInstance != null && r.ReportInstance.Report != null
                            ? r.ReportInstance.Report.Code
                            : "—",
                    PreviousStatus = StatusLabel(r.PreviousStatus),
                    NewStatus = StatusLabel(r.NewStatus),
                    Reason = r.Reason,
                    CreatedByUserName =
                        r.CreatedByUser != null
                            ? r.CreatedByUser.FirstName + " " + r.CreatedByUser.LastName
                            : "—",
                    CreatedAt = r.CreatedAt,
                })
                .ToListAsync();

            return ApiResponse<List<ReversionSummaryDto>>.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting reversions by period");
            return ApiResponse<List<ReversionSummaryDto>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error obteniendo reversiones del período"
            );
        }
    }

    private static string StatusLabel(Domain.Enums.ReportStatus status) =>
        status switch
        {
            Domain.Enums.ReportStatus.Pending => "Pendiente",
            Domain.Enums.ReportStatus.SentOnTime => "A Tiempo",
            Domain.Enums.ReportStatus.SentLate => "Tarde",
            Domain.Enums.ReportStatus.Overdue => "No Reportado",
            _ => status.ToString(),
        };
}
