using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.Notifications.Dtos;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Notifications.Services;

public interface INotificationService
{
    Task<ApiResponse<PagedResult<NotificationDto>>> GetUserNotificationsAsync(
        Guid userId,
        NotificationFilterRequest filter,
        CancellationToken ct = default
    );

    Task<ApiResponse<bool>> MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken ct = default
    );

    // Invocado por el pixel invisible del email — sin verificación de ownership
    Task MarkAsReadByPixelAsync(Guid notificationId, CancellationToken ct = default);
}

public class NotificationService(
    ApplicationDbContext db,
    INotificationRealtimeService realtimeService,
    ILogger<NotificationService> logger
) : INotificationService
{
    public async Task<ApiResponse<PagedResult<NotificationDto>>> GetUserNotificationsAsync(
        Guid userId,
        NotificationFilterRequest filter,
        CancellationToken ct = default
    )
    {
        try
        {
            var query = db
                .Notifications.Include(n => n.ReportInstance)
                    .ThenInclude(ri => ri!.Report)
                    .ThenInclude(r => r!.Branch)
                .Where(n => n.UserId == userId && n.Type == NotificationType.APP);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderBy(n => n.Readed)
                .ThenByDescending(n => n.Priority)
                .ThenByDescending(n => n.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Content,
                    Type = n.Type,
                    Severity = n.Severity,
                    Priority = n.Priority,
                    Readed = n.Readed,
                    ReadAt = n.ReadAt,
                    CreatedAt = n.CreatedAt,
                    ReportInstanceId = n.ReportInstanceId,
                    Url = n.Url,
                    BranchName = n.ReportInstance != null
                        ? n.ReportInstance.Report!.Branch!.Name
                        : null,
                })
                .ToListAsync(ct);

            return ApiResponse<PagedResult<NotificationDto>>.Ok(
                new PagedResult<NotificationDto>
                {
                    Items = items,
                    TotalItems = total,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener notificaciones para usuario {UserId}", userId);
            return ApiResponse<PagedResult<NotificationDto>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener las notificaciones."
            );
        }
    }

    public async Task<ApiResponse<bool>> MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken ct = default
    )
    {
        try
        {
            var notification = await db.Notifications.FirstOrDefaultAsync(
                n => n.Id == notificationId,
                ct
            );

            if (notification is null)
                return ApiResponse<bool>.Fail(
                    HttpStatusCode.NotFound,
                    "Notificación no encontrada."
                );

            if (notification.UserId != userId)
                return ApiResponse<bool>.Fail(
                    HttpStatusCode.Forbidden,
                    "No tienes permiso para marcar esta notificación."
                );

            if (notification.Readed)
                return ApiResponse<bool>.Ok(true);

            notification.Readed = true;
            notification.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await realtimeService.PublishReadAsync(notificationId, userId);

            return ApiResponse<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al marcar notificación {Id} como leída.", notificationId);
            return ApiResponse<bool>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al marcar la notificación como leída."
            );
        }
    }

    public async Task MarkAsReadByPixelAsync(Guid notificationId, CancellationToken ct = default)
    {
        try
        {
            var notification = await db.Notifications.FirstOrDefaultAsync(
                n => n.Id == notificationId,
                ct
            );

            if (notification is null || notification.Readed)
                return;

            notification.Readed = true;
            notification.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await realtimeService.PublishReadAsync(notificationId, notification.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en pixel tracking notificación {Id}.", notificationId);
        }
    }
}
