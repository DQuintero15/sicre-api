using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Infrastructure.Persistence;

namespace Sicre.Api.Infrastructure.Persistence.Seeders;

public static class PositionSeeder
{
    private static readonly string[] Names =
    [
        "Auditora Externa",
        "Profesional Auditoría",
        "Líder Comercialización de Gas",
        "Profesional RSE",
        "Líder Comunicaciones y RSE",
        "Líder de Contabilidad",
        "Administrador de Estaciones de Servicio",
        "Profesional de Facturación",
        "Líder de Gerencia",
        "Asistente de Gerencia",
        "Profesional Cumplimiento Normativo",
        "Líder Ambiental",
        "Coordinador HSE",
        "Líder SIG",
        "Líder Construcciones",
        "Coordinador Recaudo y Cartera",
        "Director Regulación y Tarifas",
        "Director Soporte Empresarial",
        "Coordinador Tesorería",
        "Líder Sr Atención y Gestión del Cliente",
        "Líder Operación GN y Emergencias",
        "Coordinador administrativo Instalaciones",
        "Líder de Mantenimiento GN",
        "Líder Gestión de Proyectos & Innovación",
        "Gerente Comercial",
        "Líder Corporativo de Auditoría Interna",
        "Director Contable",
        "Gerente Técnico",
        "Coordinador Estaciones de Servicio",
        "Líder de Recaudo y Cartera",
        "Director Gestión Integral",
        "Líder Sr. Infraestructura",
        "Líder Sr. Instalaciones",
        "Líder Sr. Mantenimiento",
        "Gerente Financiera",
        "Gerente Legal y Regulatorio",
        "Director Tesorería",
        "Director de Operaciones",
    ];

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var existing = await context.Positions.Select(p => p.Name).ToHashSetAsync();

        var toInsert = Names
            .Where(n => !existing.Contains(n))
            .Select(n => new Position { Id = Guid.NewGuid(), Name = n })
            .ToList();

        if (toInsert.Count == 0)
            return;

        await context.Positions.AddRangeAsync(toInsert);
        await context.SaveChangesAsync();
    }
}
