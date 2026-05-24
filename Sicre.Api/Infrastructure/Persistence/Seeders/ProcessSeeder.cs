using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Infrastructure.Persistence;

namespace Sicre.Api.Infrastructure.Persistence.Seeders;

public static class ProcessSeeder
{
    private static readonly string[] Names =
    [
        "Auditoría Externa",
        "Auditoría Interna",
        "Comercialización de gas",
        "Comunicaciones y RSE",
        "Contabilidad",
        "Estaciones de Servicio",
        "Facturación",
        "Facturación, Recaudo y Cartera",
        "Gerencia",
        "Gestión Ambiental",
        "Gestión HSE",
        "Infraestructura",
        "Recaudo y Cartera",
        "Regulación y Tarifas",
        "Soporte Empresarial",
        "Tesorería",
        "Servicio al Cliente",
        "Distribución",
        "Instalaciones",
        "Ingeniería y Mantenimiento",
        "Proyectos",
        "Comercial",
        "Técnico",
        "Financiero",
        "Gestión Legal y Regulatoria",
        "Operaciones",
        "Emergencias",
        "Operación GN",
        "Atención al Cliente",
        "Comercialización de Gas y Otras Energias",
        "Mantenimiento",
        "Tesorería y Presupuesto",
    ];

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var existing = await context.Processes.Select(p => p.Name).ToHashSetAsync();

        var toInsert = Names
            .Where(n => !existing.Contains(n))
            .Select(n => new Process { Id = Guid.NewGuid(), Name = n })
            .ToList();

        if (toInsert.Count == 0)
            return;

        await context.Processes.AddRangeAsync(toInsert);
        await context.SaveChangesAsync();
    }
}
