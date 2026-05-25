using Microsoft.AspNetCore.SignalR;
using Sicre.Api.Features.Notifications.Dtos;
using Sicre.Api.Hubs;

namespace Sicre.Api.Features.Notifications.Services;

public interface INotificationRealtimeService
{
    Task PublishCreatedAsync(NotificationDto notification, Guid userId);
    Task PublishReadAsync(Guid notificationId, Guid userId);
}

public class NotificationRealtimeService(
    IHubContext<NotificationHub> hubContext
) : INotificationRealtimeService
{
    public Task PublishCreatedAsync(NotificationDto notification, Guid userId) =>
        hubContext.Clients
            .Group($"user:{userId}")
            .SendAsync("notification_created", notification);

    public Task PublishReadAsync(Guid notificationId, Guid userId) =>
        hubContext.Clients
            .Group($"user:{userId}")
            .SendAsync("notification_read", new { id = notificationId, readed = true });
}
