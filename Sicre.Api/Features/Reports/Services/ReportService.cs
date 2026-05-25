using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.ReportInstances.Dtos.Responses;
using Sicre.Api.Features.Reports.Dtos.Requests;
using Sicre.Api.Features.Reports.Dtos.Responses;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Reports.Services;

public interface IReportService
{
    Task<ApiResponse<PagedResult<ReportSummaryResponse>>> GetAllAsync(
        GetReportsRequest request,
        CancellationToken ct = default
    );

    Task<ApiResponse<ReportResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<ApiResponse<ReportResponse>> CreateAsync(
        CreateReportRequest request,
        Guid createdByUserId,
        CancellationToken ct = default
    );

    Task<ApiResponse<ReportResponse>> UpdateAsync(
        Guid id,
        UpdateReportRequest request,
        Guid updatedByUserId,
        CancellationToken ct = default
    );

    Task<ApiResponse<bool>> DeactivateAsync(
        Guid id,
        Guid updatedByUserId,
        CancellationToken ct = default
    );
}

public class ReportService(
    ApplicationDbContext db,
    ILogger<ReportService> logger,
    IReportInstanceGenerator generator,
    IDateHelper dateHelper
) : IReportService
{
    public async Task<ApiResponse<PagedResult<ReportSummaryResponse>>> GetAllAsync(
        GetReportsRequest request,
        CancellationToken ct = default
    )
    {
        try
        {
            var query = db
                .Reports.Include(r => r.ControlEntity)
                .Include(r => r.Branch)
                .Include(r => r.Process)
                .Include(r => r.Instances)
                    .ThenInclude(i => i.ResponsibleUser)
                .Include(r => r.Instances)
                    .ThenInclude(i => i.SupervisorUser)
                .AsQueryable();

            if (request.ControlEntityId.HasValue)
                query = query.Where(r => r.ControlEntityId == request.ControlEntityId.Value);

            if (request.BranchId.HasValue)
                query = query.Where(r => r.BranchId == request.BranchId.Value);

            if (request.ProcessId.HasValue)
                query = query.Where(r => r.ProcessId == request.ProcessId.Value);

            if (request.Frequency.HasValue)
                query = query.Where(r => r.Frequency == request.Frequency.Value);

            if (request.GenerationMode.HasValue)
                query = query.Where(r => r.GenerationMode == request.GenerationMode.Value);

            if (request.DueDateRuleType.HasValue)
                query = query.Where(r => r.DueDateRuleType == request.DueDateRuleType.Value);

            if (request.IsActive.HasValue)
                query = query.Where(r => r.IsActive == request.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.ToLower();
                query = query.Where(r =>
                    r.Code.ToLower().Contains(search) || r.Name.ToLower().Contains(search)
                );
            }

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderBy(r => r.Name)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            return ApiResponse<PagedResult<ReportSummaryResponse>>.Ok(
                new PagedResult<ReportSummaryResponse>
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
            logger.LogError(ex, "Error al obtener reportes");
            return ApiResponse<PagedResult<ReportSummaryResponse>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener los reportes."
            );
        }
    }

    public async Task<ApiResponse<ReportResponse>> GetByIdAsync(
        Guid id,
        CancellationToken ct = default
    )
    {
        try
        {
            var report = await db
                .Reports.Include(r => r.ControlEntity)
                .Include(r => r.Branch)
                .Include(r => r.Process)
                .Include(r => r.SenderResponsibleUser)
                .Include(r => r.EntityUploadResponsibleUser)
                .Include(r => r.FollowUpLeaderUser)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (report == null)
                return ApiResponse<ReportResponse>.Fail(
                    HttpStatusCode.NotFound,
                    "Reporte no encontrado."
                );

            return ApiResponse<ReportResponse>.Ok(ToResponse(report));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener reporte {Id}", id);
            return ApiResponse<ReportResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener el reporte."
            );
        }
    }

    public async Task<ApiResponse<ReportResponse>> CreateAsync(
        CreateReportRequest request,
        Guid createdByUserId,
        CancellationToken ct = default
    )
    {
        try
        {
            var duplicate = await db.Reports.AnyAsync(
                r =>
                    r.Code == request.Code
                    && r.ControlEntityId == request.ControlEntityId
                    && r.BranchId == request.BranchId,
                ct
            );

            if (duplicate)
                return ApiResponse<ReportResponse>.Fail(
                    HttpStatusCode.Conflict,
                    "Ya existe un reporte con este código para la entidad de control y sede seleccionadas."
                );

            var report = new Report
            {
                Code = request.Code,
                Name = request.Name,
                ControlEntityId = request.ControlEntityId,
                ProcessId = request.ProcessId,
                BranchId = request.BranchId,
                LegalBasis = request.LegalBasis,
                Frequency = request.Frequency,
                GenerationMode = request.GenerationMode,
                DueDateRuleType = request.DueDateRuleType,
                DueDateDay = request.DueDateDay,
                DueDateMonth = request.DueDateMonth,
                DueDateDatesDefinition = request.DueDateDatesDefinition,
                OriginalDueDateText = request.OriginalDueDateText,
                AlertEarlyDays = request.AlertEarlyDays,
                AlertFollowUpDays = request.AlertFollowUpDays,
                AlertCriticalDays = request.AlertCriticalDays,
                FormatTypes = request.FormatTypes,
                InstructionsUrl = request.InstructionsUrl,
                TemplateFileUrl = request.TemplateFileUrl,
                NotificationEmails = request.NotificationEmails,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                SenderResponsibleUserId = request.SenderResponsibleUserId,
                EntityUploadResponsibleUserId = request.EntityUploadResponsibleUserId,
                FollowUpLeaderUserId = request.FollowUpLeaderUserId,
                IsActive = true,
                CreatedByUserId = createdByUserId,
            };

            db.Reports.Add(report);
            await db.SaveChangesAsync(ct);

            try
            {
                await GenerateInitialProjectionAsync(report, createdByUserId, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error generando proyección inicial para reporte {Code}",
                    request.Code
                );
            }

            await db.Entry(report).Reference(r => r.ControlEntity).LoadAsync(ct);
            await db.Entry(report).Reference(r => r.Branch).LoadAsync(ct);
            await db.Entry(report).Reference(r => r.Process).LoadAsync(ct);
            await db.Entry(report).Reference(r => r.SenderResponsibleUser).LoadAsync(ct);
            await db.Entry(report).Reference(r => r.EntityUploadResponsibleUser).LoadAsync(ct);
            await db.Entry(report).Reference(r => r.FollowUpLeaderUser).LoadAsync(ct);

            return ApiResponse<ReportResponse>.Ok(
                ToResponse(report),
                "Reporte creado exitosamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear reporte {Code}", request.Code);
            return ApiResponse<ReportResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al crear el reporte."
            );
        }
    }

    public async Task<ApiResponse<ReportResponse>> UpdateAsync(
        Guid id,
        UpdateReportRequest request,
        Guid updatedByUserId,
        CancellationToken ct = default
    )
    {
        try
        {
            var report = await db
                .Reports.Include(r => r.ControlEntity)
                .Include(r => r.Branch)
                .Include(r => r.Process)
                .Include(r => r.SenderResponsibleUser)
                .Include(r => r.EntityUploadResponsibleUser)
                .Include(r => r.FollowUpLeaderUser)
                .FirstOrDefaultAsync(r => r.Id == id, ct);

            if (report == null)
                return ApiResponse<ReportResponse>.Fail(
                    HttpStatusCode.NotFound,
                    "Reporte no encontrado."
                );

            if (request.Code != null)
                report.Code = request.Code;
            if (request.Name != null)
                report.Name = request.Name;
            if (request.LegalBasis != null)
                report.LegalBasis = request.LegalBasis;
            if (request.Frequency.HasValue)
                report.Frequency = request.Frequency.Value;
            if (request.GenerationMode.HasValue)
                report.GenerationMode = request.GenerationMode.Value;
            if (request.DueDateRuleType.HasValue)
                report.DueDateRuleType = request.DueDateRuleType.Value;
            if (request.DueDateDay.HasValue)
                report.DueDateDay = request.DueDateDay;
            if (request.DueDateMonth.HasValue)
                report.DueDateMonth = request.DueDateMonth;
            if (request.DueDateDatesDefinition != null)
                report.DueDateDatesDefinition = request.DueDateDatesDefinition;
            if (request.OriginalDueDateText != null)
                report.OriginalDueDateText = request.OriginalDueDateText;
            if (request.AlertEarlyDays.HasValue)
                report.AlertEarlyDays = request.AlertEarlyDays.Value;
            if (request.AlertFollowUpDays.HasValue)
                report.AlertFollowUpDays = request.AlertFollowUpDays.Value;
            if (request.AlertCriticalDays.HasValue)
                report.AlertCriticalDays = request.AlertCriticalDays.Value;
            if (request.FormatTypes != null)
                report.FormatTypes = request.FormatTypes;
            if (request.InstructionsUrl != null)
                report.InstructionsUrl = request.InstructionsUrl;
            if (request.TemplateFileUrl != null)
                report.TemplateFileUrl = request.TemplateFileUrl;
            if (request.NotificationEmails != null)
                report.NotificationEmails = request.NotificationEmails;
            if (request.StartDate.HasValue)
                report.StartDate = request.StartDate.Value;
            if (request.EndDate.HasValue)
                report.EndDate = request.EndDate;
            if (request.SenderResponsibleUserId.HasValue)
                report.SenderResponsibleUserId = request.SenderResponsibleUserId.Value;
            if (request.EntityUploadResponsibleUserId.HasValue)
                report.EntityUploadResponsibleUserId = request.EntityUploadResponsibleUserId.Value;
            if (request.FollowUpLeaderUserId.HasValue)
                report.FollowUpLeaderUserId = request.FollowUpLeaderUserId.Value;

            report.UpdatedAt = DateTime.UtcNow;
            report.UpdatedByUserId = updatedByUserId;

            await db.SaveChangesAsync(ct);

            return ApiResponse<ReportResponse>.Ok(
                ToResponse(report),
                "Reporte actualizado exitosamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al actualizar reporte {Id}", id);
            return ApiResponse<ReportResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al actualizar el reporte."
            );
        }
    }

    public async Task<ApiResponse<bool>> DeactivateAsync(
        Guid id,
        Guid updatedByUserId,
        CancellationToken ct = default
    )
    {
        try
        {
            var report = await db.Reports.FindAsync([id], ct);

            if (report == null)
                return ApiResponse<bool>.Fail(HttpStatusCode.NotFound, "Reporte no encontrado.");

            report.IsActive = false;
            report.UpdatedAt = DateTime.UtcNow;
            report.UpdatedByUserId = updatedByUserId;

            await db.SaveChangesAsync(ct);

            return ApiResponse<bool>.Ok(true, "Reporte desactivado exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al desactivar reporte {Id}", id);
            return ApiResponse<bool>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al desactivar el reporte."
            );
        }
    }

    private async Task GenerateInitialProjectionAsync(
        Report report,
        Guid createdByUserId,
        CancellationToken ct
    )
    {
        var settings = await db.SICRESettings.FirstOrDefaultAsync(ct);
        if (settings?.GoLiveDate is null)
            return;

        if (
            report.GenerationMode != ReportGenerationMode.Automatic
            || report.Frequency == ReportFrequency.Eventual
            || report.DueDateRuleType == ReportDueDateRuleType.ManualDateRequired
        )
            return;

        var today = dateHelper.GetCurrentDate();
        var candidates = generator.GetCandidatesInWindow(
            report,
            today,
            today.AddMonths(12),
            settings.GoLiveDate.Value
        );

        foreach (var candidate in candidates)
        {
            var status = candidate.DueDate < today ? ReportStatus.Overdue : ReportStatus.Pending;
            db.ReportInstances.Add(
                new ReportInstance
                {
                    Id = Guid.NewGuid(),
                    ReportId = report.Id,
                    PeriodYear = candidate.PeriodYear,
                    PeriodMonth = candidate.PeriodMonth,
                    PeriodName = candidate.PeriodName,
                    PeriodStart = candidate.PeriodStart,
                    PeriodEnd = candidate.PeriodEnd,
                    DueDate = candidate.DueDate,
                    EventDate = candidate.EventDate,
                    Status = status,
                    ResponsibleUserId = report.EntityUploadResponsibleUserId,
                    SupervisorUserId = report.FollowUpLeaderUserId,
                    CreatedByUserId = createdByUserId,
                    CreatedAt = DateTime.UtcNow,
                }
            );
        }

        if (candidates.Count > 0)
            await db.SaveChangesAsync(ct);
    }

    private static ReportResponse ToResponse(Report r) =>
        new()
        {
            Id = r.Id,
            Code = r.Code,
            Name = r.Name,
            ControlEntityId = r.ControlEntityId,
            ControlEntityName = r.ControlEntity?.Name,
            ProcessId = r.ProcessId,
            ProcessName = r.Process?.Name,
            BranchId = r.BranchId,
            BranchName = r.Branch?.Name,
            LegalBasis = r.LegalBasis,
            Frequency = r.Frequency,
            GenerationMode = r.GenerationMode,
            DueDateRuleType = r.DueDateRuleType,
            DueDateDay = r.DueDateDay,
            DueDateMonth = r.DueDateMonth,
            DueDateDatesDefinition = r.DueDateDatesDefinition,
            OriginalDueDateText = r.OriginalDueDateText,
            AlertEarlyDays = r.AlertEarlyDays,
            AlertFollowUpDays = r.AlertFollowUpDays,
            AlertCriticalDays = r.AlertCriticalDays,
            FormatTypes = r.FormatTypes,
            InstructionsUrl = r.InstructionsUrl,
            TemplateFileUrl = r.TemplateFileUrl,
            NotificationEmails = r.NotificationEmails,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            SenderResponsibleUserId = r.SenderResponsibleUserId,
            SenderResponsibleUserName =
                r.SenderResponsibleUser != null
                    ? $"{r.SenderResponsibleUser.FirstName} {r.SenderResponsibleUser.LastName}"
                    : null,
            EntityUploadResponsibleUserId = r.EntityUploadResponsibleUserId,
            EntityUploadResponsibleUserName =
                r.EntityUploadResponsibleUser != null
                    ? $"{r.EntityUploadResponsibleUser.FirstName} {r.EntityUploadResponsibleUser.LastName}"
                    : null,
            FollowUpLeaderUserId = r.FollowUpLeaderUserId,
            FollowUpLeaderUserName =
                r.FollowUpLeaderUser != null
                    ? $"{r.FollowUpLeaderUser.FirstName} {r.FollowUpLeaderUser.LastName}"
                    : null,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
        };

    private static ReportSummaryResponse ToSummary(Report r)
    {
        var instances = r.Instances.OrderBy(i => i.DueDate).ToList();
        return new()
        {
            Id = r.Id,
            Code = r.Code,
            Name = r.Name,
            ControlEntityId = r.ControlEntityId,
            ControlEntityName = r.ControlEntity?.Name,
            BranchId = r.BranchId,
            BranchName = r.Branch?.Name,
            ProcessId = r.ProcessId,
            ProcessName = r.Process?.Name,
            Frequency = r.Frequency,
            GenerationMode = r.GenerationMode,
            DueDateRuleType = r.DueDateRuleType,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt,
            TotalInstances = instances.Count,
            PendingInstances = instances.Count(i => i.Status == ReportStatus.Pending),
            OverdueInstances = instances.Count(i => i.Status == ReportStatus.Overdue),
            CompletedInstances = instances.Count(i =>
                i.Status == ReportStatus.SentOnTime || i.Status == ReportStatus.SentLate
            ),
            Instances = instances
                .Select(i => new ReportInstanceSummaryResponse
                {
                    Id = i.Id,
                    ReportId = i.ReportId,
                    PeriodName = i.PeriodName,
                    PeriodYear = i.PeriodYear,
                    PeriodMonth = i.PeriodMonth,
                    DueDate = i.DueDate,
                    Status = i.Status,
                    EventDate = i.EventDate,
                    SentDate = i.SentDate,
                    ResponsibleUserId = i.ResponsibleUserId,
                    ResponsibleUserName =
                        i.ResponsibleUser != null
                            ? $"{i.ResponsibleUser.FirstName} {i.ResponsibleUser.LastName}"
                            : null,
                    SupervisorUserId = i.SupervisorUserId,
                    SupervisorUserName =
                        i.SupervisorUser != null
                            ? $"{i.SupervisorUser.FirstName} {i.SupervisorUser.LastName}"
                            : null,
                    CreatedAt = i.CreatedAt,
                })
                .ToList(),
        };
    }
}
