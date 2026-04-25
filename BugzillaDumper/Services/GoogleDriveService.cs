using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Drive.v3.Data;
using File = Google.Apis.Drive.v3.Data.File;

namespace BugzillaDumper.Services;

public class GoogleDriveService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _folderId;
    private DriveService? _service;

    public GoogleDriveService(string clientId, string clientSecret, string folderId)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _folderId = folderId;
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_service != null) return;

        if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
        {
            throw new InvalidOperationException("Google Client ID or Secret is missing in settings.");
        }

        var credentialPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BugzillaDumper", "token.json");

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets { ClientId = _clientId, ClientSecret = _clientSecret },
            [DriveService.Scope.DriveFile],
            "user",
            CancellationToken.None,
            new FileDataStore(credentialPath, true));

        _service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "BugzillaDumper"
        });
    }

    public async Task<string> UploadVideoAsync(string fileName, byte[] data, string contentType)
    {
        await EnsureAuthenticatedAsync();

        var fileMetadata = new File
        {
            Name = fileName,
            Parents = string.IsNullOrEmpty(_folderId) ? null : [_folderId]
        };

        using var stream = new MemoryStream(data);
        var request = _service!.Files.Create(fileMetadata, stream, contentType);
        request.Fields = "id, webViewLink";
        request.SupportsAllDrives = true; // 支援共用雲端硬碟
        
        var progress = await request.UploadAsync();
        if (progress.Status == Google.Apis.Upload.UploadStatus.Failed)
        {
            var ex = progress.Exception;
            if (ex.Message.Contains("File not found", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(_folderId))
            {
                throw new Exception($"指定的 Google Drive 資料夾 ID ({_folderId}) 不存在或無權限存取（如果是共用雲端硬碟，請確認權限）。");
            }
            throw ex;
        }

        var uploadedFile = request.ResponseBody;
        
        // Ensure the file is readable by anyone with the link
        try
        {
            var permission = new Permission { Type = "anyone", Role = "reader" };
            var permissionRequest = _service.Permissions.Create(permission, uploadedFile.Id);
            permissionRequest.SupportsAllDrives = true; // 支援共用雲端硬碟
            await permissionRequest.ExecuteAsync();
        }
        catch { /* Ignore if company policy prevents public sharing */ }

        return uploadedFile.WebViewLink;
    }
}