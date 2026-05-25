using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Features.Auth.Controllers;
using Sicre.Api.Features.Notifications.Dtos;
using Sicre.Api.Features.Notifications.Services;
using Sicre.Api.Infrastructure.Attributes;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Notifications.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[RequireTokenType(Constants.TokenTypes.AccessToken)]
public class NotificationController(INotificationService notificationService) : BaseController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> GetAll(
        [FromQuery] NotificationFilterRequest filter,
        CancellationToken ct
    )
    {
        var result = await notificationService.GetUserNotificationsAsync(GetUserId(), filter, ct);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/mark-as-read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(Guid id, CancellationToken ct)
    {
        var result = await notificationService.MarkAsReadAsync(id, GetUserId(), ct);
        return FromResult(result);
    }

    // Pixel de apertura embebido en emails — AllowAnonymous para que los clientes de correo no requieran token
    [HttpGet("{id:guid}/mark-as-read")]
    [AllowAnonymous]
    public async Task<IActionResult> PixelMarkAsRead(Guid id, CancellationToken ct)
    {
        await notificationService.MarkAsReadByPixelAsync(id, ct);
        return File(_transparentGif, "image/gif");
    }

    // 1x1 GIF transparente
    private static readonly byte[] _transparentGif =
    [
        0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00,
        0x80, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x21,
        0xF9, 0x04, 0x01, 0x00, 0x00, 0x00, 0x00, 0x2C, 0x00, 0x00,
        0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02, 0x44,
        0x01, 0x00, 0x3B,
    ];
}
