using Microsoft.Extensions.Logging;
using Mud.Feishu;
using Mud.Feishu.Interfaces;
using Mud.Feishu.DataModels.Drive.Files;
using FeishuFileServer.Models;

namespace FeishuFileServer.Services.Feishu;

public interface IFeishuDriveService
{
    Task<FileRecord> UploadFileAsync(Stream fileStream, string fileName, string? folderToken = null, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadFileAsync(string fileToken, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string fileToken, CancellationToken cancellationToken = default);
    Task<List<FileRecord>> GetFilesAsync(string? folderToken = null, CancellationToken cancellationToken = default);
}

public class FeishuDriveService : IFeishuDriveService
{
    private readonly IFeishuTenantV1DriveFiles _driveFiles;
    private readonly ILogger<FeishuDriveService> _logger;

    public FeishuDriveService(
        IFeishuTenantV1DriveFiles driveFiles,
        ILogger<FeishuDriveService> logger)
    {
        _driveFiles = driveFiles;
        _logger = logger;
    }

    public async Task<FileRecord> UploadFileAsync(Stream fileStream, string fileName, string? folderToken = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading file {FileName} to folder {FolderToken}", fileName, folderToken);

        fileStream.Position = 0;
        var uploadRequest = new UploadAllFileRequest();

        var result = await _driveFiles.UploadAllFileAsync(uploadRequest, cancellationToken);

        if (result?.Data == null)
        {
            throw new Exception($"Failed to upload file to Feishu: {result?.Msg}");
        }

        var fileToken = result.Data.FileToken;
        _logger.LogInformation("File uploaded successfully with token: {FileToken}", fileToken);

        var fileInfo = new FileRecord
        {
            FileToken = fileToken,
            FolderToken = folderToken,
            FileName = fileName,
            FileSize = fileStream.Length,
            MimeType = GetMimeType(fileName),
            UploadTime = DateTime.UtcNow
        };

        return fileInfo;
    }

    public async Task<byte[]> DownloadFileAsync(string fileToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading file {FileToken}", fileToken);

        var content = await _driveFiles.DownloadFileAsync(fileToken, cancellationToken: cancellationToken);

        if (content == null)
        {
            throw new Exception($"Failed to download file {fileToken}");
        }

        return content;
    }

    public async Task DeleteFileAsync(string fileToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting file {FileToken}", fileToken);

        var result = await _driveFiles.DeleteFileByFileTokenAsync(fileToken, "file", cancellationToken);

        if (result?.Code != 0 && result?.Code != null)
        {
            throw new Exception($"Failed to delete file: {result?.Msg}");
        }
    }

    public async Task<List<FileRecord>> GetFilesAsync(string? folderToken = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting files from folder {FolderToken}", folderToken);

        return new List<FileRecord>();
    }

    private string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".tiff" => "image/tiff",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
