using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.ReportInstances.Dtos.Requests;
using Sicre.Api.Features.ReportInstances.Dtos.Responses;
using Sicre.Api.Features.Reports.Services;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.ReportInstances.Services;

public interface IReportInstanceService
{
    Task<ApiResponse<PagedResult<ReportInstanceSummaryResponse>>> GetAllAsync(
        GetReportInstancesRequest request,
        CancellationToken ct = default
    );

    Task<ApiResponse<ReportInstanceResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<ApiResponse<ReportInstanceResponse>> CreateManualAsync(
        CreateManualReportInstanceRequest request,
        Guid createdByUserId,
        CancellationToken ct = default
    );

    Task<ApiResponse<IReadOnlyList<ReportInstanceCandidate>>> GetPreviewAsync(
        Guid reportId,
        CancellationToken ct = default
    );

    Task<ApiResponse<ReportInstanceResponse>> UpdateAsync(
        Guid id,
        UpdateReportInstanceRequest request,
        Guid updatedByUserId,
        CancellationToken ct = default
    );

    Task<ApiResponse<ReportInstanceResponse>> RevertAsync(
        Guid id,
        RevertReportInstanceRequest request,
        Guid revertedByUserId,
        CancellationToken ct = default
    );
}

public class ReportInstanceService(
    ApplicationDbContext db,
    ILogger<ReportInstanceService> logger,
    IReportInstanceGenerator generator
) : IReportInstanceService
{
    public async Task<ApiResponse<PagedResult<ReportInstanceSummaryResponse>>> GetAllAsync(
        GetReportInstancesRequest request,
        CancellationToken ct = default
    )
    {
        try
        {
            var query = db.ReportInstances.Include(ri => ri.Report).AsQueryable();

            if (request.ReportId.HasValue)
                query = query.Where(ri => ri.ReportId == request.ReportId.Value);

            if (request.ControlEntityId.HasValue)
                query = query.Where(ri =>
                    ri.Report!.ControlEntityId == request.ControlEntityId.Value
                );

            if (request.Status.HasValue)
                query = query.Where(ri => ri.Status == request.Status.Value);

            if (request.PeriodYear.HasValue)
                query = query.Where(ri => ri.PeriodYear == request.PeriodYear.Value);

            if (request.PeriodMonth.HasValue)
                query = query.Where(ri => ri.PeriodMonth == request.PeriodMonth.Value);

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(ri => ri.PeriodYear)
                .ThenByDescending(ri => ri.PeriodMonth)
                .ThenBy(ri => ri.DueDate)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            return ApiResponse<PagedResult<ReportInstanceSummaryResponse>>.Ok(
                new PagedResult<ReportInstanceSummaryResponse>
                {
                    Items = items.Select(ToSummary).ToList(),
                    TotalItems = total,
                    Page = request.Page,
                    PageSize = request.PageSize,
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener instancias de reportes");
            return ApiResponse<PagedResult<ReportInstanceSummaryResponse>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener las instancias de reportes."
            );
        }
    }

    public async Task<ApiResponse<ReportInstanceResponse>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default
    )
    {
        try
        {
            var instance = await db
                .ReportInstances.Include(ri => ri.Report)
                .Include(ri => ri.ResponsibleUser)
                .Include(ri => ri.SupervisorUser)
                .FirstOrDefaultAsync(ri => ri.Id == id, ct);

            if (instance is null)
                return ApiResponse<ReportInstanceResponse>.Fail(
                    HttpStatusCode.NotFound,
                    "Instancia de reporte no encontrada."
                );

            return ApiResponse<ReportInstanceResponse>.Ok(ToResponse(instance));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener instancia de reporte {Id}", id);
            return ApiResponse<ReportInstanceResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener la instancia de reporte."
            );
        }
    }

    public async Task<ApiResponse<ReportInstanceResponse>> CreateManualAsync(
        CreateManualReportInstanceRequest request,
        Guid createdByUserId,
        CancellationToken ct = default
    )
    {
        try
        {
            var report = await db.Reports.FindAsync([request.ReportId], ct);

            if (report is null)
                return ApiResponse<ReportInstanceResponse>.Fail(
                    HttpStatusCode.NotFound,
                    "Reporte no encontrado."
                );

            if (!report.IsActive)
                return ApiResponse<ReportInstanceResponse>.Fail(
                    HttpStatusCode.BadRequest,
                    "No se puede crear una instancia para un reporte inactivo."
                );

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            DateOnly dueDate;
            DateOnly periodStart;
            DateOnly periodEnd;
            string periodName;

            if (request.DueDateOverride.HasValue)
            {
                dueDate = request.DueDateOverride.Value;

                var (ps, pe, pn) = ResolvePeriodInfo(
                    report,
                    request.PeriodYear,
                    request.PeriodMonth,
                    today
                );
                periodStart = ps;
                periodEnd = pe;
                periodName = pn;
            }
            else
            {
                var windowStart = new DateOnly(request.PeriodYear, 1, 1);
                var windowEnd = new DateOnly(request.PeriodYear, 12, 31);
                // Manual creation: GoLiveDate filter does not apply, pass MinValue to bypass it.
                var candidates = generator.GetCandidatesInWindow(
                    report,
                    windowStart,
                    windowEnd,
                    DateOnly.MinValue
                );

                ReportInstanceCandidate? match = request.PeriodMonth.HasValue
                    ? candidates.FirstOrDefault(c =>
                        c.PeriodYear == request.PeriodYear
                        && c.PeriodMonth == request.PeriodMonth.Value
                    )
                    : candidates.FirstOrDefault(c => c.PeriodYear == request.PeriodYear);

                if (match is not null)
                {
                    dueDate = match.DueDate;
                    periodStart = match.PeriodStart;
                    periodEnd = match.PeriodEnd;
                    periodName = match.PeriodName;
                }
                else
                {
                    var (ps, pe, pn) = ResolvePeriodInfo(
                        report,
                        request.PeriodYear,
                        request.PeriodMonth,
                        today
                    );
                    periodStart = ps;
                    periodEnd = pe;
                    periodName = pn;
                    dueDate = today;
                }
            }

            var status = dueDate < today ? ReportStatus.Overdue : ReportStatus.Pending;

            var instance = new ReportInstance
            {
                Id = Guid.NewGuid(),
                ReportId = request.ReportId,
                PeriodYear = request.PeriodYear,
                PeriodMonth = request.PeriodMonth,
                PeriodName = periodName,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                DueDate = dueDate,
                EventDate = request.EventDate,
                Status = status,
                ManualActivationReason = request.ManualActivationReason,
                DueDateOverrideReason = request.DueDateOverrideReason,
                ResponsibleUserId = report.SenderResponsibleUserId,
                SupervisorUserId = report.FollowUpLeaderUserId,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
            };

            db.ReportInstances.Add(instance);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
                when (ex.InnerException?.Message.Contains("IX_ReportInstances_Unique") == true)
            {
                return ApiResponse<ReportInstanceResponse>.Fail(
                    HttpStatusCode.Conflict,
                    "Ya existe una instancia para este reporte en el período indicado."
                );
            }

            await db.Entry(instance).Reference(ri => ri.Report).LoadAsync(ct);
            await db.Entry(instance).Reference(ri => ri.ResponsibleUser).LoadAsync(ct);
            await db.Entry(instance).Reference(ri => ri.SupervisorUser).LoadAsync(ct);

            return ApiResponse<ReportInstanceResponse>.Ok(
                ToResponse(instance),
                "Instancia de reporte creada exitosamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error al crear instancia manual de reporte {ReportId}",
                request.ReportId
            );
            return ApiResponse<ReportInstanceResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al crear la instancia de reporte."
            );
        }
    }

    public async Task<ApiResponse<IReadOnlyList<ReportInstanceCandidate>>> GetPreviewAsync(
        Guid reportId,
        CancellationToken ct = default
    )
    {
        try
        {
            var report = await db.Reports.FindAsync([reportId], ct);

            if (report is null)
                return ApiResponse<IReadOnlyList<ReportInstanceCandidate>>.Fail(
                    HttpStatusCode.NotFound,
                    "Reporte no encontrado."
                );

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var settings = await db.SICRESettings.FirstOrDefaultAsync(ct);
            var goLiveDate = settings?.GoLiveDate ?? DateOnly.MinValue;
            var candidates = generator.GetCandidatesInWindow(
                report,
                today,
                today.AddMonths(12),
                goLiveDate
            );

            return ApiResponse<IReadOnlyList<ReportInstanceCandidate>>.Ok(candidates);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error al obtener preview de instancias para reporte {ReportId}",
                reportId
            );
            return ApiResponse<IReadOnlyList<ReportInstanceCandidate>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener el preview de instancias."
            );
        }
    }

    public async Task<ApiResponse<ReportInstanceResponse>> UpdateAsync(
        Guid id,
        UpdateReportInstanceRequest request,
        Guid updatedByUserId,
        CancellationToken ct = default
    )
    {
        try
        {
            var instance = await db
                .ReportInstances.Include(ri => ri.Report)
                .Include(ri => ri.ResponsibleUser)
                .Include(ri => ri.SupervisorUser)
                .FirstOrDefaultAsync(ri => ri.Id == id, ct);

            if (instance is null)
                return ApiResponse<ReportInstanceResponse>.Fail(
                    HttpStatusCode.NotFound,
                    "Instancia de reporte no encontrada."
                );

            if (instance.Status is ReportStatus.SentOnTime or ReportStatus.SentLate)
                return ApiResponse<ReportInstanceResponse>.Fail(
                    HttpStatusCode.BadRequest,
                    "No se puede modificar una instancia ya enviada."
                );

            if (request.DueDate.HasValue)
            {
                instance.DueDate = request.DueDate.Value;
                instance.DueDateOverrideReason = request.DueDateOverrideReason;
            }

            if (request.Status.HasValue)
                instance.Status = request.Status.Value;

            if (request.SentDate.HasValue)
                instance.SentDate = request.SentDate.Value;

            if (request.DelayReason is not null)
                instance.DelayReason = request.DelayReason;

            if (request.Observations is not null)
                instance.Observations = request.Observations;

            if (request.ResponsibleUserId.HasValue)
                instance.ResponsibleUserId = request.ResponsibleUserId.Value;

            if (request.SupervisorUserId.HasValue)
                instance.SupervisorUserId = request.SupervisorUserId.Value;

            instance.UpdatedAt = DateTime.UtcNow;
            instance.UpdatedByUserId = updatedByUserId;

            await db.SaveChangesAsync(ct);

            return ApiResponse<ReportInstanceResponse>.Ok(
                ToResponse(instance),
                "Instancia de reporte actualizada exitosamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al actualizar instancia de reporte {Id}", id);
            return ApiResponse<ReportInstanceResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al actualizar la instancia de reporte."
            );
        }
    }

    public async Task<ApiResponse<ReportInstanceResponse>> RevertAsync(
        Guid id,
        RevertReportInstanceRequest request,
        Guid revertedByUserId,
        CancellationToken ct = default
    )
    {
        try
        {
            var instance = await db
                .ReportInstances.Include(ri => ri.Report)
                .Include(ri => ri.ResponsibleUser)
                .Include(ri => ri.SupervisorUser)
                .FirstOrDefaultAsync(ri => ri.Id == id, ct);

            if (instance is null)
                return ApiResponse<ReportInstanceResponse>.Fail(
                    HttpStatusCode.NotFound,
                    "Instancia de reporte no encontrada."
                );

            if (instance.Status is not ReportStatus.SentOnTime and not ReportStatus.SentLate)
                return ApiResponse<ReportInstanceResponse>.Fail(
                    HttpStatusCode.BadRequest,
                    "Solo se pueden revertir instancias con estado Enviado a tiempo o Enviado tarde."
                );

            var previousStatus = instance.Status;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var newStatus = instance.DueDate < today ? ReportStatus.Overdue : ReportStatus.Pending;

            instance.SentDate = null;
            instance.DelayReason = null;
            instance.Status = newStatus;
            instance.Observations = string.IsNullOrWhiteSpace(instance.Observations)
                ? $"Revertido el {DateTime.UtcNow:yyyy-MM-dd}: {request.Reason}"
                : $"{instance.Observations}\nRevertido el {DateTime.UtcNow:yyyy-MM-dd}: {request.Reason}";
            instance.UpdatedAt = DateTime.UtcNow;
            instance.UpdatedByUserId = revertedByUserId;

            var reversion = new ReportReversion
            {
                Id = Guid.NewGuid(),
                ReportInstanceId = instance.Id,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                Reason = request.Reason,
                CreatedByUserId = revertedByUserId,
                CreatedAt = DateTime.UtcNow,
            };

            db.ReportReversions.Add(reversion);
            await db.SaveChangesAsync(ct);

            return ApiResponse<ReportInstanceResponse>.Ok(
                ToResponse(instance),
                "Instancia de reporte revertida exitosamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al revertir instancia de reporte {Id}", id);
            return ApiResponse<ReportInstanceResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al revertir la instancia de reporte."
            );
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private static (DateOnly periodStart, DateOnly periodEnd, string periodName) ResolvePeriodInfo(
        Report report,
        int periodYear,
        int? periodMonth,
        DateOnly today
    )
    {
        if (report.Frequency == ReportFrequency.Eventual)
        {
            var eventDay = today;
            return (eventDay, eventDay, $"Eventual {periodYear}");
        }

        if (periodMonth.HasValue)
        {
            var month = periodMonth.Value;
            return report.Frequency switch
            {
                ReportFrequency.Monthly or ReportFrequency.MonthlyAnticipated => (
                    new DateOnly(periodYear, month, 1),
                    new DateOnly(periodYear, month, DateTime.DaysInMonth(periodYear, month)),
                    $"{SpanishMonths[month]} {periodYear}"
                ),
                ReportFrequency.Quarterly => (
                    new DateOnly(periodYear, month, 1),
                    new DateOnly(
                        periodYear,
                        month + 2,
                        DateTime.DaysInMonth(periodYear, month + 2)
                    ),
                    $"T{((month - 1) / 3) + 1} {periodYear}"
                ),
                ReportFrequency.SemiAnnual => (
                    new DateOnly(periodYear, month, 1),
                    new DateOnly(
                        periodYear,
                        month + 5,
                        DateTime.DaysInMonth(periodYear, month + 5)
                    ),
                    $"S{(month <= 6 ? 1 : 2)} {periodYear}"
                ),
                _ => (
                    new DateOnly(periodYear, 1, 1),
                    new DateOnly(periodYear, 12, 31),
                    $"Anual {periodYear}"
                ),
            };
        }

        return (
            new DateOnly(periodYear, 1, 1),
            new DateOnly(periodYear, 12, 31),
            $"Anual {periodYear}"
        );
    }

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

    private static ReportInstanceResponse ToResponse(ReportInstance ri) =>
        new()
        {
            Id = ri.Id,
            ReportId = ri.ReportId,
            ReportCode = ri.Report?.Code,
            ReportName = ri.Report?.Name,
            PeriodYear = ri.PeriodYear,
            PeriodMonth = ri.PeriodMonth,
            PeriodName = ri.PeriodName,
            PeriodStart = ri.PeriodStart,
            PeriodEnd = ri.PeriodEnd,
            DueDate = ri.DueDate,
            EventDate = ri.EventDate,
            SentDate = ri.SentDate,
            Status = ri.Status,
            DelayReason = ri.DelayReason,
            Observations = ri.Observations,
            ManualActivationReason = ri.ManualActivationReason,
            DueDateOverrideReason = ri.DueDateOverrideReason,
            ResponsibleUserId = ri.ResponsibleUserId,
            ResponsibleUserName = ri.ResponsibleUser is not null
                ? $"{ri.ResponsibleUser.FirstName} {ri.ResponsibleUser.LastName}"
                : null,
            SupervisorUserId = ri.SupervisorUserId,
            SupervisorUserName = ri.SupervisorUser is not null
                ? $"{ri.SupervisorUser.FirstName} {ri.SupervisorUser.LastName}"
                : null,
            CreatedAt = ri.CreatedAt,
            UpdatedAt = ri.UpdatedAt,
        };

    private static ReportInstanceSummaryResponse ToSummary(ReportInstance ri) =>
        new()
        {
            Id = ri.Id,
            ReportId = ri.ReportId,
            ReportCode = ri.Report?.Code,
            ReportName = ri.Report?.Name,
            PeriodName = ri.PeriodName,
            PeriodYear = ri.PeriodYear,
            PeriodMonth = ri.PeriodMonth,
            DueDate = ri.DueDate,
            Status = ri.Status,
            EventDate = ri.EventDate,
            SentDate = ri.SentDate,
            CreatedAt = ri.CreatedAt,
        };
}
