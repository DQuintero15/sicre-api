using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.Reports.Services;
using Sicre.Api.Infrastructure.Persistence;

namespace Sicre.Api.Infrastructure.Jobs;

public interface IReportGenerationJobService
{
    Task RunAsync();
}

public class ReportGenerationJobService(
    ApplicationDbContext db,
    ILogger<ReportGenerationJobService> logger,
    IReportInstanceGenerator generator
) : IReportGenerationJobService
{
    public async Task RunAsync()
    {
        var ct = CancellationToken.None;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        logger.LogInformation("ReportGenerationJob iniciado. Fecha UTC: {Today}", today);

        var settings = await db.SICRESettings.FirstOrDefaultAsync(ct);
        if (settings?.GoLiveDate is null)
        {
            logger.LogInformation(
                "ReportGenerationJob: GoLiveDate no configurado. No se procesará ningún reporte."
            );
            return;
        }

        // PASO 1 — Marcar instancias Pending vencidas como Overdue
        var overdueCount = await db
            .ReportInstances.Where(i => i.Status == ReportStatus.Pending && i.DueDate < today)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, ReportStatus.Overdue), ct);

        logger.LogInformation(
            "ReportGenerationJob PASO 1: {Count} instancias marcadas como Overdue.",
            overdueCount
        );

        // PASO 2 — Generación rolling de nuevas instancias
        var reports = await db
            .Reports.Where(r =>
                r.IsActive
                && r.GenerationMode == ReportGenerationMode.Automatic
                && r.Frequency != ReportFrequency.Eventual
                && r.DueDateRuleType != ReportDueDateRuleType.DaysAfterEvent
                && r.DueDateRuleType != ReportDueDateRuleType.ManualDateRequired
            )
            .ToListAsync(ct);

        logger.LogInformation(
            "ReportGenerationJob PASO 2: {Count} reportes autogenerables a procesar.",
            reports.Count
        );

        foreach (var report in reports)
        {
            try
            {
                var latest = await db
                    .ReportInstances.Where(i => i.ReportId == report.Id)
                    .OrderByDescending(i => i.PeriodYear)
                    .ThenByDescending(i => i.PeriodMonth)
                    .FirstOrDefaultAsync(ct);

                var candidate = generator.GetNextCandidate(report, latest, settings.GoLiveDate.Value);
                if (candidate is null)
                    continue;

                var status =
                    candidate.DueDate < today ? ReportStatus.Overdue : ReportStatus.Pending;

                var instance = new ReportInstance
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
                    CreatedByUserId = report.CreatedByUserId,
                    CreatedAt = DateTime.UtcNow,
                };

                db.ReportInstances.Add(instance);
                await db.SaveChangesAsync(ct);

                logger.LogInformation(
                    "ReportGenerationJob: instancia creada para reporte {ReportId} — {PeriodName} (Status: {Status}).",
                    report.Id,
                    candidate.PeriodName,
                    status
                );
            }
            catch (DbUpdateException ex)
                when (ex.InnerException?.Message.Contains("IX_ReportInstances") == true)
            {
                db.ChangeTracker.Clear();
                logger.LogWarning(
                    "ReportGenerationJob: instancia duplicada ignorada para reporte {ReportId}. Constraint: IX_ReportInstances.",
                    report.Id
                );
            }
            catch (Exception ex)
            {
                db.ChangeTracker.Clear();
                logger.LogError(
                    ex,
                    "ReportGenerationJob: error inesperado procesando reporte {ReportId}. Se continúa con el siguiente.",
                    report.Id
                );
            }
        }

        logger.LogInformation("ReportGenerationJob completado.");
    }
}
