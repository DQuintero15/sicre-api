using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Features.Notifications.Services;
using Sicre.Api.Features.ReportInstances.Dtos.Responses;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.ReportInstances.Services;

public interface IReportInstanceNoteService
{
    Task<ApiResponse<IReadOnlyList<ReportInstanceNoteResponse>>> GetByInstanceAsync(
        Guid instanceId,
        CancellationToken ct = default
    );

    Task<ApiResponse<ReportInstanceNoteResponse>> AddNoteAsync(
        Guid instanceId,
        string content,
        Guid authorUserId,
        string authorRole,
        CancellationToken ct = default
    );
}

public class ReportInstanceNoteService(
    ApplicationDbContext db,
    ILogger<ReportInstanceNoteService> logger,
    INotificationAlertService alertService
) : IReportInstanceNoteService
{
    public async Task<ApiResponse<IReadOnlyList<ReportInstanceNoteResponse>>> GetByInstanceAsync(
        Guid instanceId,
        CancellationToken ct = default
    )
    {
        try
        {
            var exists = await db.ReportInstances.AnyAsync(i => i.Id == instanceId, ct);
            if (!exists)
                return ApiResponse<IReadOnlyList<ReportInstanceNoteResponse>>.Fail(
                    HttpStatusCode.NotFound,
                    "Instancia no encontrada."
                );

            var notes = await db.ReportInstanceNotes
                .Include(n => n.AuthorUser)
                .Where(n => n.ReportInstanceId == instanceId && n.IsActive)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new ReportInstanceNoteResponse
                {
                    Id = n.Id,
                    Content = n.Content,
                    AuthorName = n.AuthorUser != null
                        ? $"{n.AuthorUser.FirstName} {n.AuthorUser.LastName}"
                        : "Usuario",
                    AuthorInitials = n.AuthorUser != null
                        ? BuildInitials(n.AuthorUser.FirstName, n.AuthorUser.LastName)
                        : "U",
                    AuthorRole = n.AuthorRole,
                    CreatedAt = n.CreatedAt,
                })
                .ToListAsync(ct);

            return ApiResponse<IReadOnlyList<ReportInstanceNoteResponse>>.Ok(notes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener notas de instancia {InstanceId}.", instanceId);
            return ApiResponse<IReadOnlyList<ReportInstanceNoteResponse>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener las notas."
            );
        }
    }

    public async Task<ApiResponse<ReportInstanceNoteResponse>> AddNoteAsync(
        Guid instanceId,
        string content,
        Guid authorUserId,
        string authorRole,
        CancellationToken ct = default
    )
    {
        try
        {
            var instanceExists = await db.ReportInstances.AnyAsync(i => i.Id == instanceId, ct);
            if (!instanceExists)
                return ApiResponse<ReportInstanceNoteResponse>.Fail(
                    HttpStatusCode.NotFound,
                    "Instancia no encontrada."
                );

            var author = await db.Users.FindAsync([authorUserId], ct);
            if (author is null)
                return ApiResponse<ReportInstanceNoteResponse>.Fail(
                    HttpStatusCode.BadRequest,
                    "Usuario no encontrado."
                );

            var note = new ReportInstanceNote
            {
                Id = Guid.NewGuid(),
                ReportInstanceId = instanceId,
                Content = content.Trim(),
                AuthorUserId = authorUserId,
                AuthorRole = authorRole,
                CreatedAt = DateTime.UtcNow,
            };

            db.ReportInstanceNotes.Add(note);
            await db.SaveChangesAsync(ct);

            await alertService.NotifyInstanceEventAsync(instanceId, "NoteAdded", authorUserId, ct);

            return ApiResponse<ReportInstanceNoteResponse>.Ok(new ReportInstanceNoteResponse
            {
                Id = note.Id,
                Content = note.Content,
                AuthorName = $"{author.FirstName} {author.LastName}",
                AuthorInitials = BuildInitials(author.FirstName, author.LastName),
                AuthorRole = note.AuthorRole,
                CreatedAt = note.CreatedAt,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al agregar nota a instancia {InstanceId}.", instanceId);
            return ApiResponse<ReportInstanceNoteResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al guardar la nota."
            );
        }
    }

    private static string BuildInitials(string firstName, string lastName)
    {
        var initials = string.Empty;
        if (!string.IsNullOrWhiteSpace(firstName)) initials += char.ToUpper(firstName.Trim()[0]);
        if (!string.IsNullOrWhiteSpace(lastName)) initials += char.ToUpper(lastName.Trim()[0]);
        return initials.Length > 0 ? initials : "U";
    }
}
