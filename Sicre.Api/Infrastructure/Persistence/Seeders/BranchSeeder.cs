using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Infrastructure.Persistence;

namespace Sicre.Api.Infrastructure.Persistence.Seeders;

public static class BranchSeeder
{
    private static readonly string[] DefaultBranches = ["Llanogas", "Cusianagas"];

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        foreach (var name in DefaultBranches)
        {
            if (!await context.Branches.AnyAsync(b => b.Name == name))
                context.Branches.Add(
                    new Branch
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        IsActive = true,
                    }
                );
        }
        await context.SaveChangesAsync();
    }
}
