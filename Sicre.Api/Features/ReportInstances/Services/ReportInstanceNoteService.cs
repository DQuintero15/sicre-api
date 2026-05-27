using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Features.ReportInstances.Dtos.Requests;
using Sicre.Api.Features.ReportInstances.Dtos.Responses;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.ReportInstances.Services;

public interface IReportInstanceNoteService
{
    Task<ApiResponse<IReadOnlyList<NoteResponse>>> GetByInstanceAsync(
        Guid instanceId,
        CancellationToken ct = default
    );

    Task<ApiResponse<NoteResponse>> CreateAsync(
        Guid instanceId,
        CreateNoteRequest request,
        Guid createdByUserId,
        CancellationToken ct = default
    );
}

public class ReportInstanceNoteService(
    ApplicationDbContext db,
    ILogger<ReportInstanceNoteService> logger
) : IReportInstanceNoteService
{
    public async Task<ApiResponse<IReadOnlyList<NoteResponse>>> GetByInstanceAsync(
        Guid instanceId,
        CancellationToken ct = default
    )
    {
        try
        {
            var exists = await db.ReportInstances.AnyAsync(ri => ri.Id == instanceId, ct);
            if (!exists)
                return ApiResponse<IReadOnlyList<NoteResponse>>.Fail(
                    HttpStatusCode.NotFound,
                    "Instancia de reporte no encontrada."
                );

            var notes = await db
                .ReportInstanceNotes.Include(n => n.CreatedByUser)
                .Where(n => n.ReportInstanceId == instanceId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(ct);

            return ApiResponse<IReadOnlyList<NoteResponse>>.Ok(notes.Select(ToResponse).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener notas de instancia {InstanceId}", instanceId);
            return ApiResponse<IReadOnlyList<NoteResponse>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener las notas."
            );
        }
    }

    public async Task<ApiResponse<NoteResponse>> CreateAsync(
        Guid instanceId,
        CreateNoteRequest request,
        Guid createdByUserId,
        CancellationToken ct = default
    )
    {
        try
        {
            var exists = await db.ReportInstances.AnyAsync(ri => ri.Id == instanceId, ct);
            if (!exists)
                return ApiResponse<NoteResponse>.Fail(
                    HttpStatusCode.NotFound,
                    "Instancia de reporte no encontrada."
                );

            var note = new ReportInstanceNote
            {
                Id = Guid.NewGuid(),
                ReportInstanceId = instanceId,
                Content = request.Content.Trim(),
                IsVisibleToResponsible = request.IsVisibleToResponsible,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
            };

            db.ReportInstanceNotes.Add(note);
            await db.SaveChangesAsync(ct);

            await db.Entry(note).Reference(n => n.CreatedByUser).LoadAsync(ct);

            return ApiResponse<NoteResponse>.Ok(ToResponse(note), "Nota agregada exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear nota para instancia {InstanceId}", instanceId);
            return ApiResponse<NoteResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al agregar la nota."
            );
        }
    }

    private static NoteResponse ToResponse(ReportInstanceNote n) =>
        new()
        {
            Id = n.Id,
            ReportInstanceId = n.ReportInstanceId,
            Content = n.Content,
            IsVisibleToResponsible = n.IsVisibleToResponsible,
            CreatedByUserId = n.CreatedByUserId,
            CreatedByUserName = n.CreatedByUser is not null
                ? $"{n.CreatedByUser.FirstName} {n.CreatedByUser.LastName}"
                : null,
            CreatedAt = n.CreatedAt,
        };
}
