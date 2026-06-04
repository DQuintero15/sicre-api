using System.Net;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Features.ControlEntities.Dtos;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.ControlEntities.Services;

public interface IControlEntityService
{
    Task<ApiResponse<PagedResult<ControlEntityDto>>> GetAllAsync(ControlEntityFilterDto filter);
    Task<ApiResponse<ControlEntityDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<ControlEntityDto>> CreateAsync(CreateControlEntityDto dto);
    Task<ApiResponse<ControlEntityDto>> UpdateAsync(Guid id, UpdateControlEntityDto dto);
}

public class ControlEntityService(ILogger<ControlEntityService> logger, ApplicationDbContext db)
    : IControlEntityService
{
    private static readonly HtmlSanitizer Sanitizer = new();

    public async Task<ApiResponse<ControlEntityDto>> CreateAsync(CreateControlEntityDto dto)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(dto.Nit))
            {
                var nitExists = await db.ControlEntities.AnyAsync(e => e.Nit == dto.Nit);
                if (nitExists)
                    return ApiResponse<ControlEntityDto>.Fail(
                        HttpStatusCode.Conflict,
                        "Ya existe una entidad de control con este NIT."
                    );
            }

            var entity = new ControlEntity
            {
                Name = dto.Name,
                Abbreviation = dto.Abbreviation,
                Nit = dto.Nit,
                LegalBasis = dto.LegalBasis != null ? Sanitizer.Sanitize(dto.LegalBasis) : null,
                Website = dto.Website,
            };

            db.ControlEntities.Add(entity);
            await db.SaveChangesAsync();

            return ApiResponse<ControlEntityDto>.Ok(
                ToDto(entity),
                "Entidad de control creada exitosamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear entidad de control {Name}", dto.Name);
            return ApiResponse<ControlEntityDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al crear la entidad de control."
            );
        }
    }

    public async Task<ApiResponse<ControlEntityDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var entity = await db.ControlEntities.FindAsync(id);
            if (entity == null)
                return ApiResponse<ControlEntityDto>.Fail(
                    HttpStatusCode.NotFound,
                    "Entidad de control no encontrada."
                );

            return ApiResponse<ControlEntityDto>.Ok(ToDto(entity));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener entidad de control {Id}", id);
            return ApiResponse<ControlEntityDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener la entidad de control."
            );
        }
    }

    public async Task<ApiResponse<PagedResult<ControlEntityDto>>> GetAllAsync(
        ControlEntityFilterDto filter
    )
    {
        try
        {
            var query = db.ControlEntities.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var name = filter.Name.ToLower();
                query = query.Where(e => e.Name.ToLower().Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(e =>
                    (e.Name != null && e.Name.ToLower().Contains(search))
                    || (e.Abbreviation != null && e.Abbreviation.ToLower().Contains(search))
                    || (e.Nit != null && e.Nit.Contains(search))
                );
            }

            if (!string.IsNullOrWhiteSpace(filter.Nit))
                query = query.Where(e => e.Nit != null && e.Nit.Contains(filter.Nit));

            if (filter.IsActive.HasValue)
                query = query.Where(e => e.IsActive == filter.IsActive.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(e => e.Name)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(e => new ControlEntityDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Abbreviation = e.Abbreviation,
                    Nit = e.Nit,
                    LegalBasis = e.LegalBasis,
                    Website = e.Website,
                    IsActive = e.IsActive,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.UpdatedAt,
                })
                .ToListAsync();

            return ApiResponse<PagedResult<ControlEntityDto>>.Ok(
                new PagedResult<ControlEntityDto>
                {
                    Items = items,
                    TotalItems = total,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener entidades de control");
            return ApiResponse<PagedResult<ControlEntityDto>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener las entidades de control."
            );
        }
    }

    public async Task<ApiResponse<ControlEntityDto>> UpdateAsync(
        Guid id,
        UpdateControlEntityDto dto
    )
    {
        try
        {
            var entity = await db.ControlEntities.FindAsync(id);
            if (entity == null)
                return ApiResponse<ControlEntityDto>.Fail(
                    HttpStatusCode.NotFound,
                    "Entidad de control no encontrada."
                );

            if (!string.IsNullOrWhiteSpace(dto.Nit) && dto.Nit != entity.Nit)
            {
                var nitConflict = await db.ControlEntities.AnyAsync(e =>
                    e.Nit == dto.Nit && e.Id != id
                );
                if (nitConflict)
                    return ApiResponse<ControlEntityDto>.Fail(
                        HttpStatusCode.Conflict,
                        "Ya existe otra entidad de control con este NIT."
                    );
            }

            entity.Name = dto.Name;
            entity.Abbreviation = dto.Abbreviation;
            entity.Nit = dto.Nit;
            entity.LegalBasis = dto.LegalBasis != null ? Sanitizer.Sanitize(dto.LegalBasis) : null;
            entity.Website = dto.Website;
            entity.IsActive = dto.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return ApiResponse<ControlEntityDto>.Ok(
                ToDto(entity),
                "Entidad de control actualizada exitosamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al actualizar entidad de control {Id}", id);
            return ApiResponse<ControlEntityDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al actualizar la entidad de control."
            );
        }
    }

    private static ControlEntityDto ToDto(ControlEntity e) =>
        new()
        {
            Id = e.Id,
            Name = e.Name,
            Abbreviation = e.Abbreviation,
            Nit = e.Nit,
            LegalBasis = e.LegalBasis,
            Website = e.Website,
            IsActive = e.IsActive,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
        };
}
