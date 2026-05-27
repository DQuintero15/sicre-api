using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Features.Audit.DTOs;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.Audit.Services;

public interface IAuditQueryService
{
    Task<ApiResponse<PagedResult<AuditEventResponse>>> GetAllAsync(
        GetAuditRequest request,
        CancellationToken ct = default
    );
}

public class AuditQueryService(
    ApplicationDbContext db,
    ILogger<AuditQueryService> logger
) : IAuditQueryService
{
    public async Task<ApiResponse<PagedResult<AuditEventResponse>>> GetAllAsync(
        GetAuditRequest request,
        CancellationToken ct = default
    )
    {
        try
        {
            var pageSize = Math.Min(request.PageSize, 200);

            var query = db.AuditEvents
                .Include(e => e.PerformedByUser)
                .AsQueryable();

            if (request.EntityType is not null)
                query = query.Where(e => e.EntityType == request.EntityType);

            if (request.EntityId.HasValue)
                query = query.Where(e => e.EntityId == request.EntityId.Value);

            if (request.PerformedByUserId.HasValue)
                query = query.Where(e => e.PerformedByUserId == request.PerformedByUserId.Value);

            if (request.BranchId.HasValue)
                query = query.Where(e => e.BranchId == request.BranchId.Value);

            if (request.DateFrom.HasValue)
                query = query.Where(e => e.PerformedAt >= request.DateFrom.Value);

            if (request.DateTo.HasValue)
                query = query.Where(e => e.PerformedAt <= request.DateTo.Value);

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(e => e.PerformedAt)
                .Skip((request.Page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return ApiResponse<PagedResult<AuditEventResponse>>.Ok(
                new PagedResult<AuditEventResponse>
                {
                    Items = items
                        .Select(e => new AuditEventResponse
                        {
                            Id = e.Id,
                            EntityType = e.EntityType,
                            EntityId = e.EntityId,
                            Action = e.Action,
                            OldValuesJson = e.OldValuesJson,
                            NewValuesJson = e.NewValuesJson,
                            PerformedByUserId = e.PerformedByUserId,
                            PerformedByUserName = e.PerformedByUser is not null
                                ? $"{e.PerformedByUser.FirstName} {e.PerformedByUser.LastName}"
                                : null,
                            PerformedAt = e.PerformedAt,
                            BranchId = e.BranchId,
                            MetadataJson = e.MetadataJson,
                        })
                        .ToList(),
                    TotalItems = total,
                    Page = request.Page,
                    PageSize = pageSize,
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al consultar eventos de auditoría");
            return ApiResponse<PagedResult<AuditEventResponse>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener los eventos de auditoría."
            );
        }
    }
}
