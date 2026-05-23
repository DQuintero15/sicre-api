using Microsoft.EntityFrameworkCore;
using Sicre.Api.Infrastructure.Persistence;

namespace Sicre.Api.Infrastructure.Jobs;

public interface IMaintenanceJobService
{
    Task CleanupRefreshTokensAsync();
}

public class MaintenanceJobService(ApplicationDbContext db, ILogger<MaintenanceJobService> logger)
    : IMaintenanceJobService
{
    public async Task CleanupRefreshTokensAsync()
    {
        var now = DateTime.UtcNow;
        var deleted = await db
            .RefreshTokens.Where(rt => rt.ExpiresAt <= now || rt.IsRevoked)
            .ExecuteDeleteAsync();

        logger.LogInformation(
            "Limpieza de tokens completada: {Count} registros eliminados.",
            deleted
        );
    }
}
