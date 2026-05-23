using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected Guid GetUserId()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (claim == null || !Guid.TryParse(claim.Value, out var userId))
            throw new UnauthorizedAccessException("User ID claim is missing or invalid.");
        return userId;
    }

    protected ActionResult<ApiResponse<T>> FromResult<T>(ApiResponse<T> result) =>
        result.StatusCode switch
        {
            HttpStatusCode.NotFound => NotFound(result),
            HttpStatusCode.BadRequest => BadRequest(result),
            HttpStatusCode.Unauthorized => Unauthorized(result),
            HttpStatusCode.Forbidden => StatusCode(403, result),
            HttpStatusCode.TooManyRequests => StatusCode(429, result),
            _ => Ok(result),
        };
}
