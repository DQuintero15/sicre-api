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
        Guid callerUserId,
        string callerRole,
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

    Task<ApiResponse<ReportInstanceResponse>> DeliverAsync(
        Guid id,
        DeliverRequest request,
        Guid userId,
        CancellationToken ct = default
    );
}

public class ReportInstanceService(
    ApplicationDbContext db,
    ILogger<ReportInstanceService> logger,
    IReportInstanceGenerator generator,
    IAuditLogService auditLog
) : IReportInstanceService
{
    public async Task<ApiResponse<PagedResult<ReportInstanceSummaryResponse>>> GetAllAsync(
        GetReportInstancesRequest request,
        Guid callerUserId,
        string callerRole,
        CancellationToken ct = default
    )
    {
        try
        {
            var query = db
                .ReportInstances.Include(ri => ri.Report)
                    .ThenInclude(r => r!.SenderResponsibleUser)
                .Include(ri => ri.Report)
                    .ThenInclude(r => r!.EntityUploadResponsibleUser)
                .Include(ri => ri.Report)
                    .ThenInclude(r => r!.FollowUpLeaderUser)
                .AsQueryable();

            query = ApplyRoleFilter(query, callerUserId, callerRole);

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
            var periodYear = request.DueDate.Year;
            var periodMonth = DerivePeriodMonth(report.Frequency, request.DueDate.Month);

            var (periodStart, periodEnd, periodName) = ResolvePeriodInfo(
                report,
                periodYear,
                periodMonth,
                today
            );

            var status = request.DueDate < today ? ReportStatus.Overdue : ReportStatus.Pending;

            var instance = new ReportInstance
            {
                Id = Guid.NewGuid(),
                ReportId = request.ReportId,
                PeriodYear = periodYear,
                PeriodMonth = periodMonth,
                PeriodName = periodName,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                DueDate = request.DueDate,
                EventDate = request.EventDate,
                Status = status,
                ManualActivationReason = request.ManualActivationReason,
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

            if (request.ResponsibleUserId.HasValue)
                await db.Entry(instance).Reference(ri => ri.ResponsibleUser).LoadAsync(ct);
            if (request.SupervisorUserId.HasValue)
                await db.Entry(instance).Reference(ri => ri.SupervisorUser).LoadAsync(ct);

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

            var reverterName = await GetUserNameAsync(revertedByUserId);
            await auditLog.RecordAsync(
                "Reverted",
                instance.Id,
                revertedByUserId,
                $"{reverterName} revirtió la instancia. Motivo: {request.Reason}",
                new { previousStatus = previousStatus.ToString(), newStatus = newStatus.ToString(), reason = request.Reason }
            );

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

    public async Task<ApiResponse<ReportInstanceResponse>> DeliverAsync(
        Guid id,
        DeliverRequest request,
        Guid userId,
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
                    "La instancia ya fue marcada como enviada."
                );

            var completedAttachments = await db
                .ReportAttachments.Where(a =>
                    a.ReportInstanceId == id
                    && a.IsActive
                    && a.UploadProgress == UploadProgress.Completed
                )
                .Select(a => new { a.Type, a.MimeType })
                .ToListAsync(ct);

            if (completedAttachments.Count == 0)
                return ApiResponse<ReportInstanceResponse>.Fail(
                    HttpStatusCode.BadRequest,
                    "La instancia debe tener al menos un adjunto cargado correctamente para marcarse como enviada."
                );

            var attachmentValidationMessage = ValidateAttachmentTypesForDelivery(
                instance.Report?.FormatTypes,
                completedAttachments.Select(x => (x.Type, x.MimeType))
            );
            if (attachmentValidationMessage is not null)
                return ApiResponse<ReportInstanceResponse>.Fail(
                    HttpStatusCode.BadRequest,
                    attachmentValidationMessage
                );

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var sentDate = request.SentDate ?? today;
            var isLate = sentDate > instance.DueDate;

            if (isLate && string.IsNullOrWhiteSpace(request.DelayReason))
                return ApiResponse<ReportInstanceResponse>.Fail(
                    HttpStatusCode.BadRequest,
                    "Debes indicar el motivo de demora cuando la entrega es extemporánea."
                );

            instance.SentDate = sentDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            instance.Status = isLate ? ReportStatus.SentLate : ReportStatus.SentOnTime;

            if (isLate && !string.IsNullOrWhiteSpace(request.DelayReason))
                instance.DelayReason = request.DelayReason.Trim();

            instance.UpdatedAt = DateTime.UtcNow;
            instance.UpdatedByUserId = userId;

            await db.SaveChangesAsync(ct);

            var delivererName = await GetUserNameAsync(userId);
            var statusLabel = isLate ? "tarde" : "a tiempo";
            await auditLog.RecordAsync(
                "Delivered",
                instance.Id,
                userId,
                $"{delivererName} marcó la instancia como enviada {statusLabel}.",
                isLate ? new { delayReason = request.DelayReason } : null
            );

            return ApiResponse<ReportInstanceResponse>.Ok(
                ToResponse(instance),
                isLate
                    ? "Instancia marcada como enviada tarde."
                    : "Instancia marcada como enviada a tiempo."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al entregar instancia {Id}", id);
            return ApiResponse<ReportInstanceResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al registrar el envío."
            );
        }
    }

    private static string? ValidateAttachmentTypesForDelivery(
        IReadOnlyCollection<ReportFormatType>? formatTypes,
        IEnumerable<(AttachmentType Type, string? MimeType)> attachments
    )
    {
        var reportFormatTypes = formatTypes?.Count > 0 ? formatTypes : [ReportFormatType.Any];
        if (reportFormatTypes.Contains(ReportFormatType.Any))
            return null;

        var allowedMimePrefixes = BuildAllowedMimePrefixes(reportFormatTypes);
        if (allowedMimePrefixes.Count == 0)
            return null;

        foreach (var attachment in attachments)
        {
            if (attachment.Type is not (AttachmentType.FinalReport or AttachmentType.Other))
                continue;

            if (string.IsNullOrWhiteSpace(attachment.MimeType))
                return "El adjunto final no tiene tipo de archivo identificado. Verifica los adjuntos cargados.";

            var isAllowed = allowedMimePrefixes.Any(prefix =>
                attachment.MimeType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            );

            if (!isAllowed)
                return "Hay adjuntos finales que no cumplen los tipos permitidos del reporte.";
        }

        return null;
    }

    private static HashSet<string> BuildAllowedMimePrefixes(
        IEnumerable<ReportFormatType> formatTypes
    )
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var formatType in formatTypes)
        {
            switch (formatType)
            {
                case ReportFormatType.PDF:
                    allowed.Add("application/pdf");
                    break;
                case ReportFormatType.Spreadsheet:
                    allowed.Add("application/vnd.ms-excel");
                    allowed.Add(
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    );
                    allowed.Add("text/csv");
                    break;
                case ReportFormatType.Archive:
                    allowed.Add("application/zip");
                    allowed.Add("application/x-zip-compressed");
                    allowed.Add("application/x-rar-compressed");
                    allowed.Add("application/x-7z-compressed");
                    break;
                case ReportFormatType.StructuredData:
                    allowed.Add("application/json");
                    allowed.Add("application/xml");
                    allowed.Add("text/xml");
                    allowed.Add("text/csv");
                    break;
                case ReportFormatType.WebPlatform:
                    break;
            }
        }

        return allowed;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private async Task<string> GetUserNameAsync(Guid userId)
    {
        var user = await db.Users.FindAsync(userId);
        return user is not null ? $"{user.FirstName} {user.LastName}" : "Usuario";
    }

    private static int? DerivePeriodMonth(ReportFrequency frequency, int month) =>
        frequency switch
        {
            ReportFrequency.Monthly or ReportFrequency.MonthlyAnticipated => month,
            ReportFrequency.Quarterly => ((month - 1) / 3) * 3 + 1, // 1, 4, 7, 10
            ReportFrequency.SemiAnnual => month <= 6 ? 1 : 7,
            ReportFrequency.Annual or ReportFrequency.Eventual => null,
            _ => month,
        };

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

    private static IQueryable<ReportInstance> ApplyRoleFilter(
        IQueryable<ReportInstance> query,
        Guid callerUserId,
        string callerRole
    )
    {
        if (callerRole == nameof(Role.Administrator) || callerRole == nameof(Role.Auditor))
            return query;

        if (callerRole == nameof(Role.ComplianceSupervisor))
            return query.Where(ri => ri.Report!.FollowUpLeaderUserId == callerUserId);

        // ReportResponsible: solo instancias de sus reportes asignados
        return query.Where(ri =>
            ri.Report!.SenderResponsibleUserId == callerUserId
            || ri.Report!.EntityUploadResponsibleUserId == callerUserId
        );
    }
}
