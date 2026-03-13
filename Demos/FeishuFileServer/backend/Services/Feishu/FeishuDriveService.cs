using FeishuFileServer.Models;
using Mud.Feishu;
using Mud.Feishu.DataModels.Drive.Files;

namespace FeishuFileServer.Services.Feishu;

/// <summary>
/// 飞书云盘服务接口
/// 提供与飞书云盘API交互的核心功能
/// </summary>
public interface IFeishuDriveService
{
    /// <summary>
    /// 上传文件到飞书云盘
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="folderToken">目标文件夹令牌，为空表示根目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上传后的文件记录</returns>
    Task<FileRecord> UploadFileAsync(Stream fileStream, string fileName, string? folderToken = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从飞书云盘下载文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容字节数组</returns>
    Task<byte[]> DownloadFileAsync(string fileToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从飞书云盘删除文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteFileAsync(string fileToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从飞书云盘删除文件夹
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteFolderAsync(string folderToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移动文件到目标文件夹
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="destFolderToken">目标文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task MoveFileAsync(string fileToken, string destFolderToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 复制文件到目标文件夹
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="destFolderToken">目标文件夹令牌</param>
    /// <param name="newName">新文件名，可选</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task CopyFileAsync(string fileToken, string destFolderToken, string? newName = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// 飞书云盘服务实现
/// 封装飞书云盘API的调用逻辑
/// </summary>
public class FeishuDriveService : IFeishuDriveService
{
    private readonly IFeishuTenantV1DriveFiles _driveFiles;
    private readonly IFeishuTenantV1DriveFolder _driveFolder;
    private readonly ILogger<FeishuDriveService> _logger;
    private readonly string _tempDirectory;

    /// <summary>
    /// 初始化飞书云盘服务实例
    /// </summary>
    /// <param name="driveFiles">飞书文件API</param>
    /// <param name="driveFolder">飞书文件夹API</param>
    /// <param name="logger">日志记录器</param>
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

    /// <summary>
    /// 上传文件到飞书云盘
    /// 使用临时文件作为中转，支持大文件上传
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="folderToken">目标文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件记录</returns>
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

    /// <summary>
    /// 从飞书云盘下载文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容字节数组</returns>
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

    /// <summary>
    /// 从飞书云盘删除文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteFileAsync(string fileToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting file {FileToken}", fileToken);

        var result = await _driveFiles.DeleteFileByFileTokenAsync(fileToken, "file", cancellationToken);

        if (result?.Code != 0 && result?.Code != null)
        {
            _logger.LogWarning("Delete file returned code {Code}: {Message}", result.Code, result.Msg);
        }
    }

    /// <summary>
    /// 从飞书云盘删除文件夹
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteFolderAsync(string folderToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting folder {FolderToken}", folderToken);

        var result = await _driveFiles.DeleteFileByFileTokenAsync(folderToken, "folder", cancellationToken);

        if (result?.Code != 0 && result?.Code != null)
        {
            _logger.LogWarning("Delete folder returned code {Code}: {Message}", result.Code, result.Msg);
        }
    }

    /// <summary>
    /// 移动文件到目标文件夹
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="destFolderToken">目标文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
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

    /// <summary>
    /// 复制文件到目标文件夹
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="destFolderToken">目标文件夹令牌</param>
    /// <param name="newName">新文件名</param>
    /// <param name="cancellationToken">取消令牌</param>
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

    /// <summary>
    /// 获取文件夹内的文件列表
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件记录列表</returns>
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

    /// <summary>
    /// 解析创建时间字符串
    /// 支持Unix时间戳和ISO日期格式
    /// </summary>
    /// <param name="timeStr">时间字符串</param>
    /// <returns>DateTime对象</returns>
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

    /// <summary>
    /// 根据文件扩展名获取MIME类型
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>MIME类型字符串</returns>
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
