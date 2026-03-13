using Microsoft.Extensions.Logging;
using Mud.Feishu;
using Mud.Feishu.Interfaces;
using Mud.Feishu.DataModels.Drive.Files;
using FeishuFileServer.Models;

using FeishuFileServer.Services.Feishu;

namespace FeishuFileServer.Services.Feishu;

public interface IFeishuDriveService
{
    Task<FileRecord> UploadFileAsync(Stream fileStream, string fileName, string? folderToken = null, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadFileAsync(string fileToken, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string fileToken, CancellationToken cancellationToken = default);
    Task DeleteFolderAsync(string folderToken, CancellationToken cancellationToken = default);
    Task MoveFileAsync(string fileToken, string destFolderToken, CancellationToken cancellationToken = default);
    Task CopyFileAsync(string fileToken, string destFolderToken, string? newName = null, CancellationToken cancellationToken = default);
}

public class FeishuDriveService : IFeishuDriveService
{
    private readonly IFeishuTenantV1DriveFiles _driveFiles;
    private readonly IFeishuTenantV1DriveFolder _driveFolder;
    private readonly ILogger<FeishuDriveService> _logger;
    private readonly string _tempDirectory;

    public FeishuDriveService(
        IFeishuTenantV1DriveFiles driveFiles,
        IFeishuTenantV1DriveFolder driveFolder,
        ILogger<FeishuDriveService> logger)
    {
        _driveFiles = driveFiles;
        _driveFolder = driveFolder;
        _logger = logger;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "FeishuFileServer");
        Directory.CreateDirectory(_tempDirectory);
    }

    public async Task<FileRecord> UploadFileAsync(Stream fileStream, string fileName, string? folderToken = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading file {FileName} to folder {FolderToken}", fileName, folderToken ?? "root");

        var tempFilePath = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}_{fileName}");
        try
        {
            fileStream.Position = 0;
            using (var fs = File.Create(tempFilePath))
            {
                await fileStream.CopyToAsync(fs, cancellationToken);
            }

            var fileSize = new FileInfo(tempFilePath).Length;
            var uploadRequest = new UploadAllFileRequest
            {
                FileName = fileName,
                ParentType = "explorer",
                ParentNode = folderToken ?? string.Empty,
                Size = (int)fileSize,
                FilePath = tempFilePath
            };

            var result = await _driveFiles.UploadAllFileAsync(uploadRequest, cancellationToken);

            if (result?.Data == null)
            {
                throw new Exception($"Failed to upload file to Feishu: {result?.Msg ?? "Unknown error"}");
            }

            var fileToken = result.Data.FileToken;
            _logger.LogInformation("File uploaded successfully with token: {FileToken}", fileToken);

            var fileInfo = new FileRecord
            {
                FileToken = fileToken,
                FolderToken = folderToken,
                FileName = fileName,
                FileSize = fileSize,
                MimeType = GetMimeType(fileName),
                UploadTime = DateTime.UtcNow
            };

            return fileInfo;
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
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
            _logger.LogWarning("Delete file returned code {Code}: {Message}", result.Code, result.Msg);
        }
    }

    public async Task DeleteFolderAsync(string folderToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting folder {FolderToken}", folderToken);

        var result = await _driveFiles.DeleteFileByFileTokenAsync(folderToken, "folder", cancellationToken);

        if (result?.Code != 0 && result?.Code != null)
        {
            _logger.LogWarning("Delete folder returned code {Code}: {Message}", result.Code, result.Msg);
        }
    }

    public async Task MoveFileAsync(string fileToken, string destFolderToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Moving file {FileToken} to folder {DestFolderToken}", fileToken, destFolderToken);

        var moveRequest = new MoveFileRequest
        {
            Type = "file",
            FolderToken = destFolderToken
        };

        var result = await _driveFiles.MoveFileByFileTokenAsync(moveRequest, fileToken, cancellationToken);

        if (result?.Code != 0 && result?.Code != null)
        {
            throw new Exception($"Failed to move file: {result.Msg}");
        }
    }

    public async Task CopyFileAsync(string fileToken, string destFolderToken, string? newName = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Copying file {FileToken} to folder {DestFolderToken}", fileToken, destFolderToken);

        var copyRequest = new CopyFileRequest
        {
            Name = newName ?? "Copy",
            Type = "file",
            FolderToken = destFolderToken
        };

        var result = await _driveFiles.CopyFileByFileTokenAsync(copyRequest, fileToken, cancellationToken: cancellationToken);

        if (result?.Code != 0 && result?.Code != null)
        {
            throw new Exception($"Failed to copy file: {result.Msg}");
        }
    }

    public async Task<List<FileRecord>> GetFilesAsync(string? folderToken = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting files from folder {FolderToken}", folderToken ?? "root");

        var result = await _driveFolder.GetFilesPageListAsync(folderToken, cancellationToken: cancellationToken);

        if (result?.Data?.Files == null)
        {
            return new List<FileRecord>();
        }

        return result.Data.Files.Select(f => new FileRecord
        {
            FileToken = f.Token ?? string.Empty,
            FileName = f.Name ?? string.Empty,
            FileSize = 0,
            MimeType = f.Type ?? "application/octet-stream",
            FolderToken = folderToken,
            UploadTime = ParseCreateTime(f.CreatedTime)
        }).ToList();
    }

    private DateTime ParseCreateTime(string? timeStr)
    {
        if (string.IsNullOrEmpty(timeStr))
        {
            return DateTime.UtcNow;
        }

        if (long.TryParse(timeStr, out var timestamp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }

        if (DateTime.TryParse(timeStr, out var dt))
        {
            return dt;
        }

        return DateTime.UtcNow;
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
