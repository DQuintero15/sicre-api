using Microsoft.AspNetCore.Identity;
using Sicre.Api.Domain.Enums;

namespace Sicre.Api.Infrastructure.Persistence.Seeders;

public static class RoleSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in Enum.GetNames<Role>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}
