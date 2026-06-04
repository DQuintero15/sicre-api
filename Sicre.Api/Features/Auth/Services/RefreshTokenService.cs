using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Auth.Services;

public interface IRefreshTokenService
{
    Task<ApiResponse<RefreshToken>> CreateAndSaveRefreshTokenAsync(
        Guid userId,
        string token,
        DateTime expiresAt
    );
    Task<ApiResponse<RefreshToken?>> GetActiveRefreshTokenAsync(Guid userId);
    Task<ApiResponse<bool>> RevokeRefreshTokenAsync(Guid userId);
    Task<ApiResponse<bool>> ValidateRefreshTokenAsync(Guid userId, string token);
}

public class RefreshTokenService(ILogger<RefreshTokenService> logger, ApplicationDbContext db)
    : IRefreshTokenService
{
    private const int MaxActive = 3;

    public async Task<ApiResponse<RefreshToken>> CreateAndSaveRefreshTokenAsync(
        Guid userId,
        string token,
        DateTime expiresAt
    )
    {
        try
        {
            var user = await db
                .Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
                return ApiResponse<RefreshToken>.Fail(
                    HttpStatusCode.NotFound,
                    "Usuario no encontrado."
                );

            using var tx = await db.Database.BeginTransactionAsync();

            var all = await db
                .RefreshTokens.Where(rt => rt.UserId == userId)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var expired = all.Where(t => t.ExpiresAt <= now).ToList();
            if (expired.Count > 0)
                db.RefreshTokens.RemoveRange(expired);

            var active = all.Where(t => !t.IsRevoked && t.ExpiresAt > now).ToList();
            foreach (var old in active.Skip(MaxActive - 1))
                old.IsRevoked = true;

            var newToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = BCrypt.Net.BCrypt.HashPassword(token),
                ExpiresAt = expiresAt,
                CreatedAt = now,
                IsRevoked = false,
            };

            db.RefreshTokens.Add(newToken);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            logger.LogInformation("Nuevo token de refresco creado para usuario {UserId}", userId);
            return ApiResponse<RefreshToken>.Ok(newToken, "Token de refresco creado exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear token de refresco para usuario {UserId}", userId);
            return ApiResponse<RefreshToken>.Fail(
                HttpStatusCode.InternalServerError,
                "Ocurrió un error al crear el token de refresco."
            );
        }
    }

    public async Task<ApiResponse<RefreshToken?>> GetActiveRefreshTokenAsync(Guid userId)
    {
        try
        {
            var token = await db.RefreshTokens.FirstOrDefaultAsync(rt =>
                rt.UserId == userId && rt.IsActive
            );

            if (token == null)
                return ApiResponse<RefreshToken?>.Fail(
                    HttpStatusCode.NotFound,
                    "No se encontró token de refresco activo."
                );

            return ApiResponse<RefreshToken?>.Ok(token, "Token de refresco obtenido exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener token de refresco para usuario {UserId}", userId);
            return ApiResponse<RefreshToken?>.Fail(
                HttpStatusCode.InternalServerError,
                "Ocurrió un error al obtener el token de refresco."
            );
        }
    }

    public async Task<ApiResponse<bool>> RevokeRefreshTokenAsync(Guid userId)
    {
        try
        {
            var activeTokens = await db
                .RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            if (activeTokens.Count == 0)
                return ApiResponse<bool>.Fail(
                    HttpStatusCode.NotFound,
                    "No se encontraron tokens de refresco activos para revocar."
                );

            foreach (var token in activeTokens)
                token.IsRevoked = true;

            await db.SaveChangesAsync();

            logger.LogInformation(
                "Se revocaron {Count} tokens de refresco para usuario {UserId}",
                activeTokens.Count,
                userId
            );
            return ApiResponse<bool>.Ok(true, "Tokens de refresco revocados exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error al revocar tokens de refresco para usuario {UserId}",
                userId
            );
            return ApiResponse<bool>.Fail(
                HttpStatusCode.InternalServerError,
                "Ocurrió un error al revocar los tokens de refresco."
            );
        }
    }

    public async Task<ApiResponse<bool>> ValidateRefreshTokenAsync(Guid userId, string token)
    {
        try
        {
            var now = DateTime.UtcNow;
            var candidates = await db
                .RefreshTokens.Where(rt =>
                    rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > now
                )
                .OrderByDescending(rt => rt.CreatedAt)
                .Take(MaxActive)
                .ToListAsync();

            foreach (var rt in candidates)
            {
                if (BCrypt.Net.BCrypt.Verify(token, rt.Token) && rt.IsActive)
                {
                    logger.LogInformation(
                        "Token de refresco validado para usuario {UserId}",
                        userId
                    );
                    return ApiResponse<bool>.Ok(true, "Token de refresco válido.");
                }
            }

            logger.LogWarning("Token de refresco inválido para usuario {UserId}", userId);
            return ApiResponse<bool>.Fail(
                HttpStatusCode.Unauthorized,
                "Token de refresco inválido o expirado. Inicie sesión nuevamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al validar token de refresco para usuario {UserId}", userId);
            return ApiResponse<bool>.Fail(
                HttpStatusCode.InternalServerError,
                "Ocurrió un error al validar el token. Inicie sesión nuevamente."
            );
        }
    }
}
