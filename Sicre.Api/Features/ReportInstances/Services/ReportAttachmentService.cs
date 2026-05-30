using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.GoogleDrive.Services;
using Sicre.Api.Features.Notifications.Services;
using Sicre.Api.Features.ReportInstances.Dtos.Responses;
using Sicre.Api.Infrastructure.Persistence;
using Sicre.Api.Shared;

namespace Sicre.Api.Features.ReportInstances.Services;

public interface IReportAttachmentService
{
    Task<ApiResponse<ReportAttachmentResponse>> AddFileAsync(
        Guid instanceId,
        AttachmentType type,
        Stream fileStream,
        string fileName,
        string contentType,
        Guid userId
    );

    Task<ApiResponse<ReportAttachmentResponse>> AddReversionFileAsync(
        Guid instanceId,
        Stream fileStream,
        string fileName,
        string contentType,
        Guid userId,
        string? notes
    );

    Task<ApiResponse<PagedResult<ReportAttachmentResponse>>> GetByInstanceAsync(
        Guid instanceId,
        CancellationToken ct = default
    );

    Task<ApiResponse<PagedResult<ReportAttachmentResponse>>> GetHistoryByInstanceAsync(
        Guid instanceId,
        CancellationToken ct = default
    );

    Task<UploadProgress?> GetProgressAsync(Guid attachmentId);
}

public class ReportAttachmentService(
    ApplicationDbContext db,
    IBackgroundQueueService backgroundQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<ReportAttachmentService> logger,
    IAuditLogService auditLog,
    INotificationAlertService alertService
) : IReportAttachmentService
{
    public async Task<ApiResponse<ReportAttachmentResponse>> AddFileAsync(
        Guid instanceId,
        AttachmentType type,
        Stream fileStream,
        string fileName,
        string contentType,
        Guid userId
    )
    {
        if (type == AttachmentType.ReversionEvidence)
            return ApiResponse<ReportAttachmentResponse>.Fail(
                HttpStatusCode.BadRequest,
                "Para este tipo de soporte, usa la opción de registrar reversión."
            );

        var result = await AddFileCoreAsync(instanceId, type, fileStream, fileName, contentType, userId);

        if (result.Success)
        {
            var user = await db.Users.FindAsync(userId);
            var userName = user is not null ? $"{user.FirstName} {user.LastName}" : "Usuario";
            await auditLog.RecordAsync(
                "AttachmentUploaded",
                instanceId,
                userId,
                $"{userName} subió el adjunto '{fileName}'.",
                new { type = type.ToString(), fileName }
            );
            await alertService.NotifyInstanceEventAsync(instanceId, "AttachmentUploaded", userId);
        }

        return result;
    }

    public async Task<ApiResponse<ReportAttachmentResponse>> AddReversionFileAsync(
        Guid instanceId,
        Stream fileStream,
        string fileName,
        string contentType,
        Guid userId,
        string? notes
    )
    {
        var result = await AddFileCoreAsync(
            instanceId,
            AttachmentType.ReversionEvidence,
            fileStream,
            fileName,
            contentType,
            userId
        );

        if (!result.Success || string.IsNullOrWhiteSpace(notes))
            return result;

        var instance = await db.ReportInstances.FirstOrDefaultAsync(i => i.Id == instanceId);
        if (instance is null)
            return result;

        var sanitizedNotes = notes.Trim();
        var entry = $"Reversión: {sanitizedNotes}";
        instance.Observations = string.IsNullOrWhiteSpace(instance.Observations)
            ? entry
            : $"{instance.Observations}\n{entry}";
        instance.UpdatedAt = DateTime.UtcNow;
        instance.UpdatedByUserId = userId;
        await db.SaveChangesAsync();

        return result;
    }

    private async Task<ApiResponse<ReportAttachmentResponse>> AddFileCoreAsync(
        Guid instanceId,
        AttachmentType type,
        Stream fileStream,
        string fileName,
        string contentType,
        Guid userId
    )
    {
        try
        {
            var instance = await db
                .ReportInstances.Include(i => i.Report)
                    .ThenInclude(r => r!.ControlEntity)
                .Include(i => i.Report)
                    .ThenInclude(r => r!.Branch)
                .Include(i => i.SupervisorUser)
                .FirstOrDefaultAsync(i => i.Id == instanceId);

            if (instance is null)
                return ApiResponse<ReportAttachmentResponse>.Fail(
                    HttpStatusCode.NotFound,
                    "Instancia no encontrada."
                );

            var user = await db.Users.FindAsync(userId);
            if (user is null)
                return ApiResponse<ReportAttachmentResponse>.Fail(
                    HttpStatusCode.NotFound,
                    "Usuario no encontrado."
                );

            var report = instance.Report!;
            var formattedName = BuildFileName(report.Code, instance, type, fileName);

            Directory.CreateDirectory("tempuploads");
            var tempPath = Path.Combine("tempuploads", $"{Guid.NewGuid()}_{formattedName}");

            await using (var fs = File.Create(tempPath))
                await fileStream.CopyToAsync(fs);

            var attachment = new ReportAttachment
            {
                ReportInstanceId = instanceId,
                Type = type,
                FileName = formattedName,
                MimeType = contentType,
                UploadedByUserId = userId,
                UploadProgress = UploadProgress.Pending,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            db.ReportAttachments.Add(attachment);
            await db.SaveChangesAsync();

            attachment.UploadProgress = UploadProgress.Uploading;
            await db.SaveChangesAsync();

            var attachmentId = attachment.Id;

            backgroundQueue.Enqueue(async ct =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var drive = scope.ServiceProvider.GetRequiredService<IGoogleDriveService>();
                    var scopeDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var entityFolder = await EnsureFolderAsync(
                        drive,
                        GetEntityFolderName(report),
                        null
                    );
                    var reportFolder = await EnsureFolderAsync(
                        drive,
                        $"{report.Code} - {report.Name}",
                        entityFolder
                    );
                    var yearFolder = await EnsureFolderAsync(
                        drive,
                        instance.PeriodYear.ToString(),
                        reportFolder
                    );
                    var periodFolder = await EnsureFolderAsync(
                        drive,
                        GetPeriodFolderName(instance),
                        yearFolder
                    );

                    await using var tempStream = File.OpenRead(tempPath);
                    var fileId = await drive.UploadFileAsync(
                        tempStream,
                        formattedName,
                        contentType,
                        periodFolder
                    );
                    var meta = await drive.GetFileMetadataAsync(fileId);

                    var att = await scopeDb.ReportAttachments.FirstAsync(
                        a => a.Id == attachmentId,
                        ct
                    );
                    att.GoogleFileId = fileId;
                    att.WebViewLink = meta.WebViewLink;
                    att.WebContentLink = meta.WebContentLink;
                    att.FileSize = meta.Size ?? 0;
                    att.UploadProgress = UploadProgress.Completed;
                    await scopeDb.SaveChangesAsync(ct);

                    logger.LogInformation(
                        "Adjunto {AttachmentId} subido a Drive exitosamente.",
                        attachmentId
                    );
                }
                catch (Exception ex)
                {
                    try
                    {
                        using var scope2 = scopeFactory.CreateScope();
                        var scopeDb2 =
                            scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var att = await scopeDb2.ReportAttachments.FirstOrDefaultAsync(
                            a => a.Id == attachmentId,
                            CancellationToken.None
                        );
                        if (att is not null)
                        {
                            att.UploadProgress = UploadProgress.Failed;
                            await scopeDb2.SaveChangesAsync(CancellationToken.None);
                        }
                    }
                    catch (Exception inner)
                    {
                        logger.LogWarning(
                            inner,
                            "No se pudo marcar adjunto {AttachmentId} como Failed.",
                            attachmentId
                        );
                    }

                    logger.LogError(
                        ex,
                        "Error subiendo adjunto {AttachmentId} a Drive.",
                        attachmentId
                    );
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempPath))
                            File.Delete(tempPath);
                    }
                    catch (Exception cleanupEx)
                    {
                        logger.LogWarning(
                            cleanupEx,
                            "No se pudo limpiar archivo temporal para adjunto {AttachmentId}.",
                            attachmentId
                        );
                    }
                }
            });

            return ApiResponse<ReportAttachmentResponse>.Ok(
                ToResponse(attachment, user),
                "El archivo fue recibido y se está subiendo en segundo plano."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en AddFileAsync para instancia {InstanceId}.", instanceId);
            return ApiResponse<ReportAttachmentResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al procesar el archivo."
            );
        }
    }

    public async Task<ApiResponse<PagedResult<ReportAttachmentResponse>>> GetByInstanceAsync(
        Guid instanceId,
        CancellationToken ct = default
    )
    {
        try
        {
            var instance = await db.ReportInstances.FirstOrDefaultAsync(
                i => i.Id == instanceId,
                ct
            );
            if (instance is null)
                return ApiResponse<PagedResult<ReportAttachmentResponse>>.Fail(
                    HttpStatusCode.NotFound,
                    "Instancia no encontrada."
                );

            var items = await db
                .ReportAttachments.Include(a => a.UploadedByUser)
                .Where(a => a.ReportInstanceId == instanceId && a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new ReportAttachmentResponse
                {
                    Id = a.Id,
                    ReportInstanceId = a.ReportInstanceId,
                    Type = a.Type,
                    FileName = a.FileName,
                    MimeType = a.MimeType,
                    GoogleFileId = a.GoogleFileId,
                    WebViewLink = a.WebViewLink,
                    WebContentLink = a.WebContentLink,
                    Url = a.Url,
                    FileSize = a.FileSize,
                    UploadProgress = a.UploadProgress,
                    UploadedByUserId = a.UploadedByUserId,
                    UploadedByUserName =
                        a.UploadedByUser != null
                            ? $"{a.UploadedByUser.FirstName} {a.UploadedByUser.LastName}"
                            : null,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt,
                })
                .ToListAsync(ct);

            return ApiResponse<PagedResult<ReportAttachmentResponse>>.Ok(
                new PagedResult<ReportAttachmentResponse>
                {
                    Items = items,
                    TotalItems = items.Count,
                    Page = 1,
                    PageSize = items.Count,
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener adjuntos de instancia {InstanceId}.", instanceId);
            return ApiResponse<PagedResult<ReportAttachmentResponse>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener los adjuntos."
            );
        }
    }

    public async Task<ApiResponse<PagedResult<ReportAttachmentResponse>>> GetHistoryByInstanceAsync(
        Guid instanceId,
        CancellationToken ct = default
    )
    {
        try
        {
            var instance = await db.ReportInstances.FirstOrDefaultAsync(
                i => i.Id == instanceId,
                ct
            );
            if (instance is null)
                return ApiResponse<PagedResult<ReportAttachmentResponse>>.Fail(
                    HttpStatusCode.NotFound,
                    "Instancia no encontrada."
                );

            var items = await db
                .ReportAttachments.Include(a => a.UploadedByUser)
                .Where(a => a.ReportInstanceId == instanceId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new ReportAttachmentResponse
                {
                    Id = a.Id,
                    ReportInstanceId = a.ReportInstanceId,
                    Type = a.Type,
                    FileName = a.FileName,
                    MimeType = a.MimeType,
                    GoogleFileId = a.GoogleFileId,
                    WebViewLink = a.WebViewLink,
                    WebContentLink = a.WebContentLink,
                    Url = a.Url,
                    FileSize = a.FileSize,
                    UploadProgress = a.UploadProgress,
                    UploadedByUserId = a.UploadedByUserId,
                    UploadedByUserName =
                        a.UploadedByUser != null
                            ? $"{a.UploadedByUser.FirstName} {a.UploadedByUser.LastName}"
                            : null,
                    IsActive = a.IsActive,
                    CreatedAt = a.CreatedAt,
                })
                .ToListAsync(ct);

            return ApiResponse<PagedResult<ReportAttachmentResponse>>.Ok(
                new PagedResult<ReportAttachmentResponse>
                {
                    Items = items,
                    TotalItems = items.Count,
                    Page = 1,
                    PageSize = items.Count,
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error al obtener historial de adjuntos de instancia {InstanceId}.",
                instanceId
            );
            return ApiResponse<PagedResult<ReportAttachmentResponse>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener el historial de adjuntos."
            );
        }
    }

    public async Task<UploadProgress?> GetProgressAsync(Guid attachmentId)
    {
        var a = await db
            .ReportAttachments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == attachmentId);
        return a?.UploadProgress;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static string BuildFileName(
        string code,
        ReportInstance instance,
        AttachmentType type,
        string originalFileName
    )
    {
        var ext = Path.GetExtension(originalFileName);
        var periodCode = GetPeriodCode(instance);
        var suffix = GetTypeSuffix(type);
        return $"{code.ToUpperInvariant()}_{periodCode}_{suffix}{ext}";
    }

    // File name:   RIESGO-01_2026-05_RF.pdf
    // Folder path: CIE / RIESGO-01 / 2026 / 05
    private static string GetPeriodCode(ReportInstance i)
    {
        var freq = i.Report?.Frequency ?? ReportFrequency.Monthly;
        return freq switch
        {
            ReportFrequency.Monthly
            or ReportFrequency.MonthlyAnticipated
            or ReportFrequency.Eventual => $"{i.PeriodYear}-{i.PeriodMonth:D2}",
            ReportFrequency.Quarterly => $"{i.PeriodYear}-T{Quarter(i.PeriodMonth)}",
            ReportFrequency.SemiAnnual => $"{i.PeriodYear}-S{Semi(i.PeriodMonth)}",
            ReportFrequency.Annual => i.PeriodYear.ToString(),
            _ => $"{i.PeriodYear}-{i.PeriodMonth:D2}",
        };
    }

    private static string GetPeriodFolderName(ReportInstance i)
    {
        var freq = i.Report?.Frequency ?? ReportFrequency.Monthly;
        return freq switch
        {
            ReportFrequency.Monthly
            or ReportFrequency.MonthlyAnticipated
            or ReportFrequency.Eventual => $"{i.PeriodMonth:D2}",
            ReportFrequency.Quarterly => $"T{Quarter(i.PeriodMonth)}",
            ReportFrequency.SemiAnnual => $"S{Semi(i.PeriodMonth)}",
            ReportFrequency.Annual => "Anual",
            _ => $"{i.PeriodMonth:D2}",
        };
    }

    private static string GetEntityFolderName(Report r) => r.ControlEntity?.Abbreviation ?? r.Code;

    private static string GetTypeSuffix(AttachmentType t) =>
        t switch
        {
            AttachmentType.FinalReport => "RF",
            AttachmentType.SubmissionEvidence => "EE",
            AttachmentType.DeadlineExtensionEvidence => "EA",
            AttachmentType.ReversionEvidence => "ER",
            AttachmentType.Other => "OT",
            _ => "OT",
        };

    private static int Quarter(int? month) => ((month ?? 1) - 1) / 3 + 1;

    private static int Semi(int? month) => (month ?? 1) <= 6 ? 1 : 2;

    private static async Task<string> EnsureFolderAsync(
        IGoogleDriveService drive,
        string name,
        string? parentId
    )
    {
        var id = await drive.FindFolderIdAsync(name, parentId);
        return string.IsNullOrEmpty(id) ? await drive.CreateFolderAsync(name, parentId) : id;
    }

    private static ReportAttachmentResponse ToResponse(ReportAttachment a, User user) =>
        new()
        {
            Id = a.Id,
            ReportInstanceId = a.ReportInstanceId,
            Type = a.Type,
            FileName = a.FileName,
            MimeType = a.MimeType,
            GoogleFileId = a.GoogleFileId,
            WebViewLink = a.WebViewLink,
            WebContentLink = a.WebContentLink,
            Url = a.Url,
            FileSize = a.FileSize,
            UploadProgress = a.UploadProgress,
            UploadedByUserId = a.UploadedByUserId,
            UploadedByUserName = $"{user.FirstName} {user.LastName}",
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt,
        };
}
