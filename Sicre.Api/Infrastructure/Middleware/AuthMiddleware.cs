using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Shared;

namespace Sicre.Api.Infrastructure.Middleware;

public class AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, UserManager<User> userManager)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() is not null)
        {
            await next(context);
            return;
        }

        var urlsToSkip = new[] { "/api/auth/login", "/api/auth/complete-setup" };
        if (
            urlsToSkip.Any(u =>
                context.Request.Path.StartsWithSegments(u, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            await next(context);
            return;
        }

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tokenType = context.User.FindFirst(Constants.ClaimNames.TokenType)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            await next(context);
            return;
        }

        if (tokenType == Constants.TokenTypes.AccessToken)
        {
            var hasCookie =
                context.Request.Cookies.TryGetValue(
                    Constants.CookieNames.RefreshToken,
                    out var cookieVal
                ) && !string.IsNullOrEmpty(cookieVal);

            if (!hasCookie)
            {
                logger.LogWarning(
                    "Access token sin cookie de refresco. Path: {Path}",
                    context.Request.Path
                );
                await WriteJson(
                    context,
                    HttpStatusCode.Unauthorized,
                    "Tu sesion expiro. Vuelve a iniciar sesion."
                );
                return;
            }
        }

        if (!Guid.TryParse(userId, out var userIdGuid))
        {
            logger.LogWarning("Formato de ID de usuario inválido: {UserId}", userId);
            await WriteJson(context, HttpStatusCode.Unauthorized, "Invalid user ID format");
            return;
        }

        var user = await userManager.FindByIdAsync(userIdGuid.ToString());

        if (user == null)
        {
            logger.LogWarning("Usuario no encontrado: {UserId}", userIdGuid);
            await WriteJson(context, HttpStatusCode.Unauthorized, "User not found");
            return;
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Usuario inactivo: {UserId}", userIdGuid);
            await WriteJson(context, HttpStatusCode.Forbidden, "User account is inactive");
            return;
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            var remaining = user.LockoutEnd.Value - DateTimeOffset.UtcNow;
            logger.LogWarning("Usuario bloqueado: {UserId}", userIdGuid);
            await WriteJson(
                context,
                HttpStatusCode.Forbidden,
                $"User account is locked for {remaining.TotalMinutes:F0} minutes"
            );
            return;
        }

        context.Items["User"] = user;
        await next(context);
    }

    private static Task WriteJson(HttpContext ctx, HttpStatusCode status, string message)
    {
        ctx.Response.StatusCode = (int)status;
        return ctx.Response.WriteAsJsonAsync(ApiResponse<string>.Fail(status, message));
    }
}

public static class AuthMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthMiddleware(this IApplicationBuilder app) =>
        app.UseMiddleware<AuthMiddleware>();
}
