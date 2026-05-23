using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Infrastructure.Persistence;

namespace Sicre.Api.Infrastructure.Persistence.Seeders;

public static class SICRESettingsSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (!await context.SICRESettings.AnyAsync())
        {
            context.SICRESettings.Add(
                new SICRESettings { Id = Guid.NewGuid(), AutoNotify = false }
            );
            await context.SaveChangesAsync();
        }
    }
}
