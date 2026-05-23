using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Infrastructure.Persistence;

namespace Sicre.Api.Infrastructure.Persistence.Seeders;

public static class ControlEntitySeeder
{
    private static readonly ControlEntity[] Entities =
    [
        new()
        {
            Nit = "800.250.984",
            Name = "Superintendencia de Servicios Públicos",
            Abbreviation = "SSPD",
            Website = "https://www.superservicios.gov.co",
        },
        new()
        {
            Nit = "800.134.137",
            Name = "Departamento Administrativo Nacional de Estadística",
            Abbreviation = "DANE",
            Website = "https://www.dane.gov.co",
        },
        new()
        {
            Nit = "800.104.620",
            Name = "Contraloría Municipal de Villavicencio",
            Website = "https://www.contraloriavillavicencio.gov.co",
        },
        new()
        {
            Nit = "892.000.133",
            Name = "Cámara de Comercio",
            Website = "https://www.ccv.org.co",
        },
        new()
        {
            Nit = "899.999.043",
            Name = "Ministerio de Minas y Energía",
            Website = "https://www.minenergia.gov.co",
        },
        new()
        {
            Nit = "800.245.890",
            Name = "Comisión de Regulación de Energía y Gas",
            Abbreviation = "CREG",
            Website = "https://www.creg.gov.co",
        },
        new()
        {
            Nit = "830.000.602",
            Name = "Instituto de Hidrología, Meteorología y Estudios Ambientales",
            Abbreviation = "IDEAM",
            Website = "https://www.ideam.gov.co",
        },
        new()
        {
            Nit = "822.000.316",
            Name =
                "Corporación para el Desarrollo Sostenible del área de Manejo Especial La Macarena",
            Abbreviation = "Cormacarena",
            Website = "https://www.cormacarena.gov.co",
        },
        new()
        {
            Nit = "800.251.448",
            Name = "Ministerio del Trabajo",
            Website = "https://www.mintrabajo.gov.co",
        },
        new()
        {
            Nit = "899.999.034",
            Name = "Servicio Nacional de Aprendizaje",
            Abbreviation = "SENA",
            Website = "https://www.sena.edu.co",
        },
        new()
        {
            Nit = "800.197.268",
            Name = "Dirección de Impuestos y Aduanas Nacionales",
            Abbreviation = "DIAN",
            Website = "https://www.dian.gov.co",
        },
        new()
        {
            Nit = "800.176.089",
            Name = "Superintendencia de Industria y Comercio",
            Abbreviation = "SIC",
            Website = "https://www.sic.gov.co",
        },
        new()
        {
            Nit = "899.999.086",
            Name = "Superintendencia de Sociedades",
            Website = "https://www.supersociedades.gov.co",
        },
        new()
        {
            Nit = "892.000.222",
            Name = "Empresa de Acueducto y Alcantarillado de Villavicencio",
            Abbreviation = "EAAV",
            Website = "https://www.eaav.gov.co",
        },
        new()
        {
            Nit = "844.000.755",
            Name = "La Empresa de Acueducto y Alcantarillado de Yopal E.I.C.E E.S.P.",
            Abbreviation = "EAAAY",
            Website = "https://www.eaaay.gov.co",
        },
    ];

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var existingNits = await context.ControlEntities.Select(e => e.Nit).ToHashSetAsync();

        var toInsert = Entities
            .Where(e => !existingNits.Contains(e.Nit))
            .Select(e => new ControlEntity
            {
                Id = Guid.NewGuid(),
                Nit = e.Nit,
                Name = e.Name,
                Abbreviation = e.Abbreviation,
                Website = e.Website,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            })
            .ToList();

        if (toInsert.Count == 0)
            return;

        await context.ControlEntities.AddRangeAsync(toInsert);
        await context.SaveChangesAsync();
    }
}
