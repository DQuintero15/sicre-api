using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Features.Branches.Dtos;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Branches.Services;

public interface IBranchService
{
    Task<ApiResponse<PagedResult<BranchDto>>> GetAllAsync(BranchFilterDto filter);
    Task<ApiResponse<BranchDto>> GetByIdAsync(Guid id);
    Task<ApiResponse<BranchDto>> CreateAsync(CreateBranchDto dto);
    Task<ApiResponse<BranchDto>> UpdateAsync(Guid id, UpdateBranchDto dto);
}

public class BranchService(ILogger<BranchService> logger, ApplicationDbContext db) : IBranchService
{
    public async Task<ApiResponse<BranchDto>> CreateAsync(CreateBranchDto dto)
    {
        try
        {
            var exists = await db.Branches.AnyAsync(b => b.Name.ToLower() == dto.Name.ToLower());
            if (exists)
                return ApiResponse<BranchDto>.Fail(HttpStatusCode.Conflict, "La sede ya existe.");

            var branch = new Branch { Name = dto.Name };
            db.Branches.Add(branch);
            await db.SaveChangesAsync();

            return ApiResponse<BranchDto>.Ok(ToDto(branch), "Sede creada exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear sede {Name}", dto.Name);
            return ApiResponse<BranchDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al crear la sede."
            );
        }
    }

    public async Task<ApiResponse<BranchDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var branch = await db.Branches.FindAsync(id);
            if (branch == null)
                return ApiResponse<BranchDto>.Fail(HttpStatusCode.NotFound, "Sede no encontrada.");

            return ApiResponse<BranchDto>.Ok(ToDto(branch));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener sede {Id}", id);
            return ApiResponse<BranchDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener la sede."
            );
        }
    }

    public async Task<ApiResponse<PagedResult<BranchDto>>> GetAllAsync(BranchFilterDto filter)
    {
        try
        {
            var query = db.Branches.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                var name = filter.Name.ToUpper();
                query = query.Where(b =>
                    EF.Functions.Unaccent(b.Name).ToUpper().Contains(EF.Functions.Unaccent(name))
                );
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(b => b.Name)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(b => new BranchDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    IsActive = b.IsActive,
                })
                .ToListAsync();

            return ApiResponse<PagedResult<BranchDto>>.Ok(
                new PagedResult<BranchDto>
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
            logger.LogError(ex, "Error al obtener sedes");
            return ApiResponse<PagedResult<BranchDto>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener las sedes."
            );
        }
    }

    public async Task<ApiResponse<BranchDto>> UpdateAsync(Guid id, UpdateBranchDto dto)
    {
        try
        {
            var branch = await db.Branches.FindAsync(id);
            if (branch == null)
                return ApiResponse<BranchDto>.Fail(HttpStatusCode.NotFound, "Sede no encontrada.");

            var nameConflict = await db.Branches.AnyAsync(b =>
                b.Name.ToLower() == dto.Name.ToLower() && b.Id != id
            );
            if (nameConflict)
                return ApiResponse<BranchDto>.Fail(
                    HttpStatusCode.Conflict,
                    "Ya existe otra sede con este nombre."
                );

            branch.Name = dto.Name;
            branch.IsActive = dto.IsActive;
            await db.SaveChangesAsync();

            return ApiResponse<BranchDto>.Ok(ToDto(branch), "Sede actualizada exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al actualizar sede {Id}", id);
            return ApiResponse<BranchDto>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al actualizar la sede."
            );
        }
    }

    private static BranchDto ToDto(Branch b) =>
        new()
        {
            Id = b.Id,
            Name = b.Name,
            IsActive = b.IsActive,
        };
}
