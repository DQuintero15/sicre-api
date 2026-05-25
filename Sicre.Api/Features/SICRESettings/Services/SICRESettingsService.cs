using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Features.SICRESettings.Dtos;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.SICRESettings.Services;

public interface ISICRESettingsService
{
    Task<ApiResponse<SICRESettingsResponse>> GetAsync(CancellationToken ct = default);
    Task<ApiResponse<SICRESettingsResponse>> UpdateAsync(
        UpdateSICRESettingsRequest request,
        Guid updatedByUserId,
        CancellationToken ct = default
    );
}

public class SICRESettingsService(
    ApplicationDbContext db,
    ILogger<SICRESettingsService> logger
) : ISICRESettingsService
{
    public async Task<ApiResponse<SICRESettingsResponse>> GetAsync(CancellationToken ct = default)
    {
        try
        {
            var settings = await db.SICRESettings
                .Include(s => s.UpdatedByUser)
                .FirstOrDefaultAsync(ct);

            if (settings is null)
                return ApiResponse<SICRESettingsResponse>.Fail(
                    HttpStatusCode.NotFound,
                    "Configuración SICRE no encontrada."
                );

            return ApiResponse<SICRESettingsResponse>.Ok(ToResponse(settings));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener configuración SICRE.");
            return ApiResponse<SICRESettingsResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener la configuración."
            );
        }
    }

    public async Task<ApiResponse<SICRESettingsResponse>> UpdateAsync(
        UpdateSICRESettingsRequest request,
        Guid updatedByUserId,
        CancellationToken ct = default
    )
    {
        try
        {
            var settings = await db.SICRESettings
                .Include(s => s.UpdatedByUser)
                .FirstOrDefaultAsync(ct);

            if (settings is null)
                return ApiResponse<SICRESettingsResponse>.Fail(
                    HttpStatusCode.NotFound,
                    "Configuración SICRE no encontrada."
                );

            settings.GoLiveDate = request.GoLiveDate;
            settings.AutoNotify = request.AutoNotify;
            settings.UpdatedByUserId = updatedByUserId;
            settings.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            await db.Entry(settings).Reference(s => s.UpdatedByUser).LoadAsync(ct);

            logger.LogInformation(
                "Configuración SICRE actualizada por usuario {UserId}.",
                updatedByUserId
            );

            return ApiResponse<SICRESettingsResponse>.Ok(
                ToResponse(settings),
                "Configuración actualizada exitosamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al actualizar configuración SICRE.");
            return ApiResponse<SICRESettingsResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al actualizar la configuración."
            );
        }
    }

    private static SICRESettingsResponse ToResponse(Domain.Entities.SICRESettings s) =>
        new()
        {
            GoLiveDate = s.GoLiveDate,
            AutoNotify = s.AutoNotify,
            UpdatedAt = s.UpdatedAt,
            UpdatedByUserName = s.UpdatedByUser is not null
                ? $"{s.UpdatedByUser.FirstName} {s.UpdatedByUser.LastName}"
                : null,
        };
}
