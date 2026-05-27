using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.Audit.Services;
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
        Guid currentUserId,
        Role currentUserRole,
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

    Task<ApiResponse<BulkDeliverResponse>> BulkDeliverAsync(
        BulkDeliverRequest request,
        Guid userId,
        CancellationToken ct = default
    );
}

public class ReportInstanceService(
    ApplicationDbContext db,
    ILogger<ReportInstanceService> logger,
    IReportInstanceGenerator generator,
    IAuditService auditService
) : IReportInstanceService
{
    public async Task<ApiResponse<PagedResult<ReportInstanceSummaryResponse>>> GetAllAsync(
        GetReportInstancesRequest request,
        Guid currentUserId,
        Role currentUserRole,
        CancellationToken ct = default
    )
    {
        try
        {
            var query = db.ReportInstances.Include(ri => ri.Report).AsQueryable();

            if (currentUserRole == Role.ReportResponsible)
            {
                query = query.Where(ri =>
                    ri.ResponsibleUserId == currentUserId || ri.SupervisorUserId == currentUserId
                );
            }
            else if (currentUserRole == Role.ComplianceSupervisor)
            {
                var supervisorBranchId = await db
                    .Users.Where(u => u.Id == currentUserId)
                    .Select(u => u.BranchId)
                    .FirstOrDefaultAsync(ct);

                if (supervisorBranchId.HasValue)
                    query = query.Where(ri =>
                        ri.Report != null && ri.Report.BranchId == supervisorBranchId
                    );
            }

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
                .Include(ri => ri.Reversions)
                .ThenInclude(r => r.CreatedByUser)
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

            var isReassignment = request.ResponsibleUserId.HasValue || request.SupervisorUserId.HasValue;

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

            auditService.Log(
                entityType: "ReportInstance",
                entityId: instance.Id,
                action: isReassignment ? AuditAction.Reassign : AuditAction.Update,
                performedByUserId: updatedByUserId,
                branchId: instance.Report?.BranchId
            );

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

            auditService.Log(
                entityType: "ReportInstance",
                entityId: instance.Id,
                action: AuditAction.Revert,
                performedByUserId: revertedByUserId,
                oldValues: new { status = previousStatus.ToString() },
                newValues: new { status = newStatus.ToString(), reason = request.Reason },
                branchId: instance.Report?.BranchId
            );

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

            var (validationError, isLate) = await ValidateAndPrepareDelivery(
                instance,
                request.SentDate,
                request.DelayReason,
                ct
            );

            if (validationError is not null)
                return ApiResponse<ReportInstanceResponse>.Fail(
                    HttpStatusCode.BadRequest,
                    validationError
                );

            ApplyDelivery(instance, request.SentDate, request.DelayReason, userId, isLate);

            auditService.Log(
                entityType: "ReportInstance",
                entityId: instance.Id,
                action: AuditAction.Deliver,
                performedByUserId: userId,
                newValues: new { sentDate = instance.SentDate, status = instance.Status.ToString() },
                branchId: instance.Report?.BranchId
            );

            await db.SaveChangesAsync(ct);

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

    public async Task<ApiResponse<BulkDeliverResponse>> BulkDeliverAsync(
        BulkDeliverRequest request,
        Guid userId,
        CancellationToken ct = default
    )
    {
        try
        {
            if (request.InstanceIds.Count == 0)
                return ApiResponse<BulkDeliverResponse>.Fail(
                    HttpStatusCode.BadRequest,
                    "Debes indicar al menos una instancia."
                );

            var instances = await db
                .ReportInstances.Include(ri => ri.Report)
                .Where(ri => request.InstanceIds.Contains(ri.Id))
                .ToListAsync(ct);

            var response = new BulkDeliverResponse();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            foreach (var id in request.InstanceIds)
            {
                var instance = instances.FirstOrDefault(i => i.Id == id);

                if (instance is null)
                {
                    response.Results.Add(new BulkDeliverItemResult { InstanceId = id, Success = false, Reason = "Instancia no encontrada." });
                    continue;
                }

                var (validationError, isLate) = await ValidateAndPrepareDelivery(
                    instance,
                    sentDate: null,
                    delayReason: null,
                    ct
                );

                if (validationError is not null)
                {
                    response.Results.Add(new BulkDeliverItemResult { InstanceId = id, Success = false, Reason = validationError });
                    continue;
                }

                ApplyDelivery(instance, sentDate: null, delayReason: null, userId, isLate);

                auditService.Log(
                    entityType: "ReportInstance",
                    entityId: instance.Id,
                    action: AuditAction.BulkDeliver,
                    performedByUserId: userId,
                    newValues: new { sentDate = instance.SentDate, status = instance.Status.ToString() },
                    branchId: instance.Report?.BranchId
                );

                response.Results.Add(new BulkDeliverItemResult { InstanceId = id, Success = true });
            }

            if (response.SuccessCount > 0)
                await db.SaveChangesAsync(ct);

            return ApiResponse<BulkDeliverResponse>.Ok(
                response,
                $"{response.SuccessCount} instancia(s) entregada(s), {response.FailureCount} con error."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en entrega masiva");
            return ApiResponse<BulkDeliverResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al procesar la entrega masiva."
            );
        }
    }

    private async Task<(string? error, bool isLate)> ValidateAndPrepareDelivery(
        ReportInstance instance,
        DateOnly? sentDate,
        string? delayReason,
        CancellationToken ct
    )
    {
        if (instance.Status is ReportStatus.SentOnTime or ReportStatus.SentLate)
            return ("La instancia ya fue marcada como enviada.", false);

        var completedAttachments = await db
            .ReportAttachments.Where(a =>
                a.ReportInstanceId == instance.Id
                && a.IsActive
                && a.UploadProgress == UploadProgress.Completed
            )
            .Select(a => new { a.Type, a.MimeType })
            .ToListAsync(ct);

        if (completedAttachments.Count == 0)
            return (
                "La instancia debe tener al menos un adjunto cargado correctamente para marcarse como enviada.",
                false
            );

        var attachmentError = ValidateAttachmentTypesForDelivery(
            instance.Report?.FormatTypes,
            completedAttachments.Select(x => (x.Type, x.MimeType))
        );
        if (attachmentError is not null)
            return (attachmentError, false);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveSentDate = sentDate ?? today;
        var isLate = effectiveSentDate > instance.DueDate;

        if (isLate && string.IsNullOrWhiteSpace(delayReason))
            return ("Debes indicar el motivo de demora cuando la entrega es extemporánea.", false);

        return (null, isLate);
    }

    private static void ApplyDelivery(
        ReportInstance instance,
        DateOnly? sentDate,
        string? delayReason,
        Guid userId,
        bool isLate
    )
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveSentDate = sentDate ?? today;

        instance.SentDate = effectiveSentDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        instance.Status = isLate ? ReportStatus.SentLate : ReportStatus.SentOnTime;

        if (isLate && !string.IsNullOrWhiteSpace(delayReason))
            instance.DelayReason = delayReason.Trim();

        instance.UpdatedAt = DateTime.UtcNow;
        instance.UpdatedByUserId = userId;
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
            Reversions = ri.Reversions
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReversionResponse
                {
                    Id = r.Id,
                    PreviousStatus = r.PreviousStatus,
                    NewStatus = r.NewStatus,
                    Reason = r.Reason,
                    CreatedByUserId = r.CreatedByUserId,
                    CreatedByUserName = r.CreatedByUser is not null
                        ? $"{r.CreatedByUser.FirstName} {r.CreatedByUser.LastName}"
                        : null,
                    CreatedAt = r.CreatedAt,
                })
                .ToList(),
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
