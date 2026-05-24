using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Features.Positions.Dtos;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Positions.Services;

public interface IPositionService
{
    Task<ApiResponse<PagedResult<PositionDto>>> GetAllAsync(PositionFilterDto filter);
    Task<ApiResponse<PositionDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<PositionDto>> CreateAsync(CreatePositionDto dto);
    Task<ApiResponse<PositionDto>> UpdateAsync(Guid id, UpdatePositionDto dto);
}

public class PositionService(ILogger<PositionService> logger, ApplicationDbContext db)
    : IPositionService
{
    public async Task<ApiResponse<PositionDto>> CreateAsync(CreatePositionDto dto)
    {
        try
        {
            var exists = await db.Positions.AnyAsync(p => p.Name.ToLower() == dto.Name.ToLower());
            if (exists)
                return ApiResponse<PositionDto>.Fail(
                    HttpStatusCode.Conflict,
                    "El cargo ya existe."
                );

            var position = new Position { Name = dto.Name };
            db.Positions.Add(position);
            await db.SaveChangesAsync();

            return ApiResponse<PositionDto>.Ok(ToDto(position), "Cargo creado exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear cargo {Name}", dto.Name);
            return ApiResponse<PositionDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al crear el cargo."
            );
        }
    }

    public async Task<ApiResponse<PositionDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var position = await db.Positions.FindAsync(id);
            if (position == null)
                return ApiResponse<PositionDto>.Fail(
                    HttpStatusCode.NotFound,
                    "Cargo no encontrado."
                );

            return ApiResponse<PositionDto>.Ok(ToDto(position));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener cargo {Id}", id);
            return ApiResponse<PositionDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener el cargo."
            );
        }
    }

    public async Task<ApiResponse<PagedResult<PositionDto>>> GetAllAsync(PositionFilterDto filter)
    {
        try
        {
            var query = db.Positions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var name = filter.Name.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(name));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Name)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new PositionDto { Id = p.Id, Name = p.Name })
                .ToListAsync();

            return ApiResponse<PagedResult<PositionDto>>.Ok(
                new PagedResult<PositionDto>
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
            logger.LogError(ex, "Error al obtener cargos");
            return ApiResponse<PagedResult<PositionDto>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener los cargos."
            );
        }
    }

    public async Task<ApiResponse<PositionDto>> UpdateAsync(Guid id, UpdatePositionDto dto)
    {
        try
        {
            var position = await db.Positions.FindAsync(id);
            if (position == null)
                return ApiResponse<PositionDto>.Fail(
                    HttpStatusCode.NotFound,
                    "Cargo no encontrado."
                );

            var nameConflict = await db.Positions.AnyAsync(p =>
                p.Name.ToLower() == dto.Name.ToLower() && p.Id != id
            );
            if (nameConflict)
                return ApiResponse<PositionDto>.Fail(
                    HttpStatusCode.Conflict,
                    "Ya existe otro cargo con este nombre."
                );

            position.Name = dto.Name;
            await db.SaveChangesAsync();

            return ApiResponse<PositionDto>.Ok(ToDto(position), "Cargo actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al actualizar cargo {Id}", id);
            return ApiResponse<PositionDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al actualizar el cargo."
            );
        }
    }

    private static PositionDto ToDto(Position p) => new() { Id = p.Id, Name = p.Name };
}
