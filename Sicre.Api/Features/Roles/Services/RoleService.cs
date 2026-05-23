using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.Roles.Dtos;
using Sicre.Api.Shared;
using Sicre.Api.Shared.Extensions;

namespace Sicre.Api.Features.Roles.Services;

public interface IRoleService
{
    Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync();
}

public class RoleService(RoleManager<IdentityRole<Guid>> roleManager, ILogger<RoleService> logger)
    : IRoleService
{
    public async Task<ApiResponse<List<RoleDto>>> GetAllRolesAsync()
    {
        try
        {
            var identityRoles = await roleManager.Roles.ToListAsync();
            var identityByName = identityRoles.ToDictionary(r => r.Name!, r => r);

            var roles = Enum.GetValues<Role>()
                .Where(enumRole => identityByName.ContainsKey(enumRole.ToString()))
                .Select(enumRole =>
                {
                    var identityRole = identityByName[enumRole.ToString()];
                    return new RoleDto
                    {
                        Id = identityRole.Id.ToString(),
                        Name = enumRole.ToString(),
                        DisplayName = enumRole.GetDisplayName(),
                    };
                })
                .ToList();

            return ApiResponse<List<RoleDto>>.Ok(roles);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener todos los roles");
            return ApiResponse<List<RoleDto>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener los roles."
            );
        }
    }
}
