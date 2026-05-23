using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Features.Users.Dtos;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Users.Services;

public interface IUserProfileService
{
    Task<ApiResponse<ProfileDto>> GetProfileAsync(Guid userId);
    Task<UserProfileResponseDto?> GetUserProfileDataAsync(Guid userId);
}

public class UserProfileService(
    ILogger<UserProfileService> logger,
    UserManager<User> userManager,
    ApplicationDbContext db
) : IUserProfileService
{
    public async Task<ApiResponse<ProfileDto>> GetProfileAsync(Guid userId)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return ApiResponse<ProfileDto>.Fail(
                    HttpStatusCode.NotFound,
                    "Usuario no encontrado."
                );

            var roles = await userManager.GetRolesAsync(user);

            return ApiResponse<ProfileDto>.Ok(
                new ProfileDto($"{user.FirstName} {user.LastName}", roles)
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener perfil del usuario {UserId}", userId);
            return ApiResponse<ProfileDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener el perfil del usuario."
            );
        }
    }

    public async Task<UserProfileResponseDto?> GetUserProfileDataAsync(Guid userId)
    {
        var user = await db
            .Users.Include(u => u.Position)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        var initials =
            $"{user.FirstName.FirstOrDefault()}{user.LastName.FirstOrDefault()}".ToUpper();
        var monthName = user.CreatedAt.ToString(
            "MMMM",
            new System.Globalization.CultureInfo("es-ES")
        );

        return new UserProfileResponseDto
        {
            Id = user.Id,
            Nombre = $"{user.FirstName} {user.LastName}",
            Iniciales = initials,
            MiembroDesde = $"{monthName} de {user.CreatedAt.Year}",
            CorreoElectronico = user.Email!,
            Telefono = user.PhoneNumber,
            Cargo = user.Position?.Name,
            IdCargo = user.PositionId,
            FechaCreacion = user.CreatedAt,
            UltimaActualizacion = user.LockoutEnd?.UtcDateTime,
        };
    }
}
