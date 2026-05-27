using System.Text.Json;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Infrastructure.Persistence;

namespace Sicre.Api.Features.Audit.Services;

public interface IAuditService
{
    void Log(
        string entityType,
        Guid entityId,
        string action,
        Guid performedByUserId,
        object? oldValues = null,
        object? newValues = null,
        Guid? branchId = null,
        string? metadata = null
    );
}

public class AuditService(ApplicationDbContext db) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    public void Log(
        string entityType,
        Guid entityId,
        string action,
        Guid performedByUserId,
        object? oldValues = null,
        object? newValues = null,
        Guid? branchId = null,
        string? metadata = null
    )
    {
        db.AuditEvents.Add(
            new AuditEvent
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                PerformedByUserId = performedByUserId,
                OldValuesJson = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
                NewValuesJson = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions),
                BranchId = branchId,
                MetadataJson = metadata,
                PerformedAt = DateTime.UtcNow,
            }
        );
    }
}
