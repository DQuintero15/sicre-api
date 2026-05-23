using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Features.Processes.Dtos;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Processes.Services;

public interface IProcessService
{
    Task<ApiResponse<PagedResult<ProcessDto>>> GetAllAsync(ProcessFilterDto filter);
    Task<ApiResponse<ProcessDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<ProcessDto>> CreateAsync(CreateProcessDto dto);
    Task<ApiResponse<ProcessDto>> UpdateAsync(Guid id, UpdateProcessDto dto);
}

public class ProcessService(ILogger<ProcessService> logger, ApplicationDbContext db)
    : IProcessService
{
    public async Task<ApiResponse<ProcessDto>> CreateAsync(CreateProcessDto dto)
    {
        try
        {
            var exists = await db.Processes.AnyAsync(p => p.Name.ToLower() == dto.Name.ToLower());
            if (exists)
                return ApiResponse<ProcessDto>.Fail(
                    HttpStatusCode.Conflict,
                    "El proceso ya existe."
                );

            var process = new Process { Name = dto.Name };
            db.Processes.Add(process);
            await db.SaveChangesAsync();

            return ApiResponse<ProcessDto>.Ok(ToDto(process), "Proceso creado exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear proceso {Name}", dto.Name);
            return ApiResponse<ProcessDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al crear el proceso."
            );
        }
    }

    public async Task<ApiResponse<ProcessDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var process = await db.Processes.FindAsync(id);
            if (process == null)
                return ApiResponse<ProcessDto>.Fail(
                    HttpStatusCode.NotFound,
                    "Proceso no encontrado."
                );

            return ApiResponse<ProcessDto>.Ok(ToDto(process));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener proceso {Id}", id);
            return ApiResponse<ProcessDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener el proceso."
            );
        }
    }

    public async Task<ApiResponse<PagedResult<ProcessDto>>> GetAllAsync(ProcessFilterDto filter)
    {
        try
        {
            var query = db.Processes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var name = filter.Name.ToUpper();
                query = query.Where(p =>
                    EF.Functions.Unaccent(p.Name).ToUpper().Contains(EF.Functions.Unaccent(name))
                );
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Name)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new ProcessDto { Id = p.Id, Name = p.Name })
                .ToListAsync();

            return ApiResponse<PagedResult<ProcessDto>>.Ok(
                new PagedResult<ProcessDto>
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
            logger.LogError(ex, "Error al obtener procesos");
            return ApiResponse<PagedResult<ProcessDto>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener los procesos."
            );
        }
    }

    public async Task<ApiResponse<ProcessDto>> UpdateAsync(Guid id, UpdateProcessDto dto)
    {
        try
        {
            var process = await db.Processes.FindAsync(id);
            if (process == null)
                return ApiResponse<ProcessDto>.Fail(
                    HttpStatusCode.NotFound,
                    "Proceso no encontrado."
                );

            var nameConflict = await db.Processes.AnyAsync(p =>
                p.Name.ToLower() == dto.Name.ToLower() && p.Id != id
            );
            if (nameConflict)
                return ApiResponse<ProcessDto>.Fail(
                    HttpStatusCode.Conflict,
                    "Ya existe otro proceso con este nombre."
                );

            process.Name = dto.Name;
            await db.SaveChangesAsync();

            return ApiResponse<ProcessDto>.Ok(ToDto(process), "Proceso actualizado exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al actualizar proceso {Id}", id);
            return ApiResponse<ProcessDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al actualizar el proceso."
            );
        }
    }

    private static ProcessDto ToDto(Process p) => new() { Id = p.Id, Name = p.Name };
}
