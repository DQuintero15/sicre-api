using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Features.ReportInstances.Dtos.Responses;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.ReportInstances.Services;

public interface IAuditLogService
{
    Task RecordAsync(
        string action,
        Guid instanceId,
        Guid performedByUserId,
        string humanReadable,
        object? details = null
    );

    Task<ApiResponse<IReadOnlyList<ReportInstanceActivityResponse>>> GetActivityAsync(
        Guid instanceId,
        CancellationToken ct = default
    );

    Task<ApiResponse<IReadOnlyList<ReportInstanceAuditEntryResponse>>> GetAuditAsync(
        Guid instanceId,
        CancellationToken ct = default
    );
}

public class AuditLogService(
    ApplicationDbContext db,
    ILogger<AuditLogService> logger
) : IAuditLogService
{
    public async Task RecordAsync(
        string action,
        Guid instanceId,
        Guid performedByUserId,
        string humanReadable,
        object? details = null
    )
    {
        try
        {
            var entry = new ReportInstanceAuditEntry
            {
                Id = Guid.NewGuid(),
                ReportInstanceId = instanceId,
                Action = action,
                PerformedByUserId = performedByUserId,
                HumanReadable = humanReadable,
                Details = details is not null ? JsonSerializer.Serialize(details) : null,
                CreatedAt = DateTime.UtcNow,
            };
            db.ReportInstanceAuditEntries.Add(entry);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "No se pudo registrar entrada de auditoría para instancia {InstanceId}.",
                instanceId
            );
        }
    }

    public async Task<ApiResponse<IReadOnlyList<ReportInstanceActivityResponse>>> GetActivityAsync(
        Guid instanceId,
        CancellationToken ct = default
    )
    {
        try
        {
            var exists = await db.ReportInstances.AnyAsync(i => i.Id == instanceId, ct);
            if (!exists)
                return ApiResponse<IReadOnlyList<ReportInstanceActivityResponse>>.Fail(
                    HttpStatusCode.NotFound,
                    "Instancia no encontrada."
                );

            var items = await db.ReportInstanceAuditEntries
                .Include(e => e.PerformedByUser)
                .Where(e => e.ReportInstanceId == instanceId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new ReportInstanceActivityResponse
                {
                    Id = e.Id,
                    Action = e.Action,
                    HumanReadable = e.HumanReadable,
                    PerformedByUserName = e.PerformedByUser != null
                        ? $"{e.PerformedByUser.FirstName} {e.PerformedByUser.LastName}"
                        : null,
                    CreatedAt = e.CreatedAt,
                })
                .ToListAsync(ct);

            return ApiResponse<IReadOnlyList<ReportInstanceActivityResponse>>.Ok(items);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener actividad de instancia {InstanceId}.", instanceId);
            return ApiResponse<IReadOnlyList<ReportInstanceActivityResponse>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener la actividad."
            );
        }
    }

    public async Task<ApiResponse<IReadOnlyList<ReportInstanceAuditEntryResponse>>> GetAuditAsync(
        Guid instanceId,
        CancellationToken ct = default
    )
    {
        try
        {
            var exists = await db.ReportInstances.AnyAsync(i => i.Id == instanceId, ct);
            if (!exists)
                return ApiResponse<IReadOnlyList<ReportInstanceAuditEntryResponse>>.Fail(
                    HttpStatusCode.NotFound,
                    "Instancia no encontrada."
                );

            var items = await db.ReportInstanceAuditEntries
                .Include(e => e.PerformedByUser)
                .Where(e => e.ReportInstanceId == instanceId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new ReportInstanceAuditEntryResponse
                {
                    Id = e.Id,
                    Action = e.Action,
                    HumanReadable = e.HumanReadable,
                    PerformedByUserName = e.PerformedByUser != null
                        ? $"{e.PerformedByUser.FirstName} {e.PerformedByUser.LastName}"
                        : null,
                    Details = e.Details,
                    CreatedAt = e.CreatedAt,
                })
                .ToListAsync(ct);

            return ApiResponse<IReadOnlyList<ReportInstanceAuditEntryResponse>>.Ok(items);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener auditoría de instancia {InstanceId}.", instanceId);
            return ApiResponse<IReadOnlyList<ReportInstanceAuditEntryResponse>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener la auditoría."
            );
        }
    }
}
