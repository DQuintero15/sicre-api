using System.Net;
using Microsoft.EntityFrameworkCore;
using Sicre.Api.Domain.Entities;
using Sicre.Api.Domain.Enums;
using Sicre.Api.Features.GoogleDrive.Services;
using Sicre.Api.Features.ReportInstances.Dtos.Requests;
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

    Task<ApiResponse<ReportAttachmentResponse>> AddUrlAsync(
        Guid instanceId,
        AddUrlAttachmentRequest request,
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

    Task<UploadProgress?> GetProgressAsync(Guid attachmentId);
}

public class ReportAttachmentService(
    ApplicationDbContext db,
    IBackgroundQueueService backgroundQueue,
    IServiceProvider serviceProvider,
    ILogger<ReportAttachmentService> logger
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
                    using var scope = serviceProvider.CreateScope();
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

                    File.Delete(tempPath);

                    logger.LogInformation(
                        "Adjunto {AttachmentId} subido a Drive exitosamente.",
                        attachmentId
                    );
                }
                catch (Exception ex)
                {
                    try
                    {
                        using var scope2 = serviceProvider.CreateScope();
                        var scopeDb2 =
                            scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var att = await scopeDb2.ReportAttachments.FirstOrDefaultAsync(
                            a => a.Id == attachmentId,
                            ct
                        );
                        if (att is not null)
                        {
                            att.UploadProgress = UploadProgress.Failed;
                            await scopeDb2.SaveChangesAsync(ct);
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

    public async Task<ApiResponse<ReportAttachmentResponse>> AddUrlAsync(
        Guid instanceId,
        AddUrlAttachmentRequest request,
        Guid userId
    )
    {
        try
        {
            var instance = await db.ReportInstances.FirstOrDefaultAsync(i => i.Id == instanceId);
            if (instance is null)
                return ApiResponse<ReportAttachmentResponse>.Fail(
                    HttpStatusCode.NotFound,
                    "Instancia no encontrada."
                );

            if (
                !Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            )
                return ApiResponse<ReportAttachmentResponse>.Fail(
                    HttpStatusCode.BadRequest,
                    "La URL proporcionada no es válida."
                );

            var user = await db.Users.FindAsync(userId);
            if (user is null)
                return ApiResponse<ReportAttachmentResponse>.Fail(
                    HttpStatusCode.NotFound,
                    "Usuario no encontrado."
                );

            var attachment = new ReportAttachment
            {
                ReportInstanceId = instanceId,
                Type = request.Type,
                FileName = request.Name,
                Url = request.Url.Trim(),
                UploadProgress = UploadProgress.Completed,
                UploadedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            db.ReportAttachments.Add(attachment);
            await db.SaveChangesAsync();

            return ApiResponse<ReportAttachmentResponse>.Ok(
                ToResponse(attachment, user),
                "Adjunto por URL registrado exitosamente."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en AddUrlAsync para instancia {InstanceId}.", instanceId);
            return ApiResponse<ReportAttachmentResponse>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al registrar el adjunto por URL."
            );
        }
    }

    public Task<ApiResponse<ReportAttachmentResponse>> AddReversionFileAsync(
        Guid instanceId,
        Stream fileStream,
        string fileName,
        string contentType,
        Guid userId,
        string? notes
    ) =>
        AddFileAsync(
            instanceId,
            AttachmentType.ReversionEvidence,
            fileStream,
            fileName,
            contentType,
            userId
        );

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
                    UploadedByUserName = a.UploadedByUser != null
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
                "Error al obtener adjuntos de instancia {InstanceId}.",
                instanceId
            );
            return ApiResponse<PagedResult<ReportAttachmentResponse>>.Fail(
                HttpStatusCode.InternalServerError,
                "Error al obtener los adjuntos."
            );
        }
    }

    public async Task<UploadProgress?> GetProgressAsync(Guid attachmentId)
    {
        var a = await db.ReportAttachments.AsNoTracking()
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

    private static string GetPeriodCode(ReportInstance i)
    {
        var freq = i.Report?.Frequency ?? ReportFrequency.Monthly;
        return freq switch
        {
            ReportFrequency.Monthly or ReportFrequency.MonthlyAnticipated
                => $"{i.PeriodYear}-{i.PeriodMonth:D2}",
            ReportFrequency.Quarterly => $"{i.PeriodYear}-T{GetQuarterNumber(i.PeriodMonth)}",
            ReportFrequency.SemiAnnual => $"{i.PeriodYear}-S{GetSemiNumber(i.PeriodMonth)}",
            ReportFrequency.Annual => i.PeriodYear.ToString(),
            _ => $"{i.PeriodYear}-{i.PeriodMonth:D2}",
        };
    }

    private static string GetPeriodFolderName(ReportInstance i)
    {
        var freq = i.Report?.Frequency ?? ReportFrequency.Monthly;
        return freq switch
        {
            ReportFrequency.Monthly or ReportFrequency.MonthlyAnticipated
                => $"{i.PeriodMonth:D2}_{SpanishMonth(i.PeriodMonth)}",
            ReportFrequency.Quarterly => $"T{GetQuarterNumber(i.PeriodMonth)}_Trimestre{GetQuarterNumber(i.PeriodMonth)}",
            ReportFrequency.SemiAnnual => $"S{GetSemiNumber(i.PeriodMonth)}_Semestre{GetSemiNumber(i.PeriodMonth)}",
            ReportFrequency.Annual => $"{i.PeriodYear}_Anual",
            _ => i.PeriodName,
        };
    }

    private static string GetEntityFolderName(Report r) =>
        $"{r.ControlEntity?.Abbreviation ?? ""} - {r.ControlEntity?.Name ?? r.Code}";

    private static string GetTypeSuffix(AttachmentType t) =>
        t switch
        {
            AttachmentType.FinalReport => "ReporteFinal",
            AttachmentType.SubmissionEvidence => "EvidenciaEnvio",
            AttachmentType.DeadlineExtensionEvidence => "EvidenciaAmpliacionPlazo",
            AttachmentType.ReversionEvidence => "EvidenciaReversion",
            AttachmentType.Other => "Otro",
            _ => "Anexo",
        };

    private static int GetQuarterNumber(int? month) => ((( month ?? 1) - 1) / 3) + 1;

    private static int GetSemiNumber(int? month) => (month ?? 1) <= 6 ? 1 : 2;

    private static string SpanishMonth(int? m) =>
        m switch
        {
            1 => "Enero",
            2 => "Febrero",
            3 => "Marzo",
            4 => "Abril",
            5 => "Mayo",
            6 => "Junio",
            7 => "Julio",
            8 => "Agosto",
            9 => "Septiembre",
            10 => "Octubre",
            11 => "Noviembre",
            12 => "Diciembre",
            _ => $"Mes{m}",
        };

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
