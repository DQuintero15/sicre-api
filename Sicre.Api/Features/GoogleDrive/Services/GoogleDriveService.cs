using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sicre.Api.Config;
using Sicre.Api.Infrastructure.Persistence;

namespace Sicre.Api.Features.GoogleDrive.Services;

public interface IGoogleDriveService
{
    Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string mimeType,
        string? parentId = null
    );
    Task<Stream?> DownloadFileAsync(string fileId);
    Task<string> CreateFolderAsync(string folderName, string? parentId = null);
    Task<string?> FindFolderIdAsync(string folderName, string? parentId = null);
    Task<Google.Apis.Drive.v3.Data.File> GetFileMetadataAsync(string fileId);
}

public class GoogleDriveService : IGoogleDriveService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _applicationName;
    private readonly string _rootFolderId;

    public GoogleDriveService(IOptions<AppSettings> options, IServiceScopeFactory scopeFactory)
    {
        var s = options.Value.GoogleDrive;
        _clientId = s.ClientId;
        _clientSecret = s.ClientSecret;
        _applicationName = s.ApplicationName;
        _rootFolderId = s.FolderId;
        _scopeFactory = scopeFactory;
    }

    private async Task<DriveService> GetDriveServiceAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var token =
            await db.GoogleDriveTokens.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException(
                "Google Drive no está configurado. Conecta la cuenta desde la configuración del sistema."
            );

        var flow = new GoogleAuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret,
                },
                Scopes = [DriveService.ScopeConstants.Drive],
            }
        );

        if (token.ExpiresAt <= DateTime.UtcNow.AddMinutes(5))
        {
            var refreshed = await flow.RefreshTokenAsync(
                "app",
                token.RefreshToken,
                CancellationToken.None
            );
            token.AccessToken = refreshed.AccessToken;
            if (!string.IsNullOrEmpty(refreshed.RefreshToken))
                token.RefreshToken = refreshed.RefreshToken;
            token.ExpiresAt = DateTime.UtcNow.AddSeconds(refreshed.ExpiresInSeconds ?? 3600);
            token.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var credential = new UserCredential(
            flow,
            "app",
            new TokenResponse
            {
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                IssuedUtc = DateTime.UtcNow,
                ExpiresInSeconds = (long)(token.ExpiresAt - DateTime.UtcNow).TotalSeconds,
            }
        );

        return new DriveService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _applicationName,
            }
        );
    }

    public async Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string mimeType,
        string? parentId = null
    )
    {
        var drive = await GetDriveServiceAsync();
        var meta = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileName,
            Parents = [parentId ?? _rootFolderId],
        };
        var request = drive.Files.Create(meta, fileStream, mimeType);
        request.Fields = "id, name, webViewLink, webContentLink";
        request.SupportsAllDrives = true;
        request.QuotaUser = "SICRE";
        var result = await request.UploadAsync();
        if (result.Status == UploadStatus.Failed)
            throw new Exception($"Error al subir archivo: {result.Exception.Message}");
        return request.ResponseBody.Id;
    }

    public async Task<Stream?> DownloadFileAsync(string fileId)
    {
        try
        {
            var drive = await GetDriveServiceAsync();
            var request = drive.Files.Get(fileId);
            request.SupportsAllDrives = true;
            var stream = new MemoryStream();
            await request.DownloadAsync(stream);
            stream.Position = 0;
            return stream;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> CreateFolderAsync(string folderName, string? parentId = null)
    {
        var drive = await GetDriveServiceAsync();
        var meta = new Google.Apis.Drive.v3.Data.File
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = [parentId ?? _rootFolderId],
        };
        var request = drive.Files.Create(meta);
        request.Fields = "id";
        request.SupportsAllDrives = true;
        var file = await request.ExecuteAsync();
        return file.Id;
    }

    public async Task<string?> FindFolderIdAsync(string folderName, string? parentId = null)
    {
        var drive = await GetDriveServiceAsync();
        var parent = parentId ?? _rootFolderId;
        var request = drive.Files.List();
        request.Q =
            $"mimeType = 'application/vnd.google-apps.folder' and name = '{folderName}' and '{parent}' in parents and trashed = false";
        request.Fields = "files(id, name)";
        request.SupportsAllDrives = true;
        var result = await request.ExecuteAsync();
        return result.Files.FirstOrDefault()?.Id;
    }

    public async Task<Google.Apis.Drive.v3.Data.File> GetFileMetadataAsync(string fileId)
    {
        var drive = await GetDriveServiceAsync();
        var request = drive.Files.Get(fileId);
        request.Fields = "id, name, webViewLink, webContentLink, size, mimeType";
        request.SupportsAllDrives = true;
        return await request.ExecuteAsync();
    }
}
