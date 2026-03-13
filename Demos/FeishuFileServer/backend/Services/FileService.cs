using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FeishuFileServer.Configuration;
using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services.Feishu;

namespace FeishuFileServer.Services;

public interface IFileService
{
    Task<FileUploadResponse> UploadFileAsync(IFormFile file, string? folderToken, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadFileAsync(string fileToken, string? versionToken = null, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string fileToken, CancellationToken cancellationToken = default);
    Task<FileInfoResponse?> GetFileAsync(string fileToken, CancellationToken cancellationToken = default);
    Task<FileListResponse> GetFilesAsync(string? folderToken = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

public class FileService : IFileService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly IFeishuDriveService _feishuDriveService;
    private readonly FileUploadSettings _uploadSettings;
    private readonly ILogger<FileService> _logger;

    public FileService(
        FeishuFileDbContext dbContext,
        IFeishuDriveService feishuDriveService,
        IOptions<FileUploadSettings> uploadSettings,
        ILogger<FileService> logger)
    {
        _dbContext = dbContext;
        _feishuDriveService = feishuDriveService;
        _uploadSettings = uploadSettings.Value;
        _logger = logger;
    }

    public async Task<FileUploadResponse> UploadFileAsync(IFormFile file, string? folderToken, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        string? fileMD5 = null;
        if (_uploadSettings.EnableDeduplication)
        {
            fileMD5 = CalculateMD5(memoryStream);
            memoryStream.Position = 0;

            var existingFile = await _dbContext.FileRecords
                .FirstOrDefaultAsync(f => f.FileMD5 == fileMD5 && !f.IsDeleted, cancellationToken);

            if (existingFile != null)
            {
                _logger.LogInformation("File with MD5 {MD5} already exists, returning existing file", fileMD5);
                return new FileUploadResponse
                {
                    FileToken = existingFile.FileToken,
                    FileName = existingFile.FileName,
                    FileSize = existingFile.FileSize,
                    MimeType = existingFile.MimeType,
                    FileMD5 = existingFile.FileMD5,
                    UploadTime = existingFile.UploadTime
                };
            }
        }

        var fileRecord = await _feishuDriveService.UploadFileAsync(memoryStream, file.FileName, folderToken, cancellationToken);

        fileRecord.FileMD5 = fileMD5;
        fileRecord.FolderToken = folderToken;

        _dbContext.FileRecords.Add(fileRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("File uploaded successfully: {FileToken}", fileRecord.FileToken);

        return new FileUploadResponse
        {
            FileToken = fileRecord.FileToken,
            FileName = fileRecord.FileName,
            FileSize = fileRecord.FileSize,
            MimeType = fileRecord.MimeType,
            FileMD5 = fileRecord.FileMD5,
            UploadTime = fileRecord.UploadTime
        };
    }

    public async Task<byte[]> DownloadFileAsync(string fileToken, string? versionToken = null, CancellationToken cancellationToken = default)
    {
        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"File with token {fileToken} not found");
        }

        return await _feishuDriveService.DownloadFileAsync(fileToken, cancellationToken);
    }

    public async Task DeleteFileAsync(string fileToken, CancellationToken cancellationToken = default)
    {
        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"File with token {fileToken} not found");
        }

        await _feishuDriveService.DeleteFileAsync(fileToken, cancellationToken);

        fileRecord.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("File deleted: {FileToken}", fileToken);
    }

    public async Task<FileInfoResponse?> GetFileAsync(string fileToken, CancellationToken cancellationToken = default)
    {
        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            return null;
        }

        return MapToFileInfoResponse(fileRecord);
    }

    public async Task<FileListResponse> GetFilesAsync(string? folderToken = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FileRecords.Where(f => !f.IsDeleted);

        if (!string.IsNullOrEmpty(folderToken))
        {
            query = query.Where(f => f.FolderToken == folderToken);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var files = await query
            .OrderByDescending(f => f.UploadTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new FileListResponse
        {
            Files = files.Select(MapToFileInfoResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_uploadSettings.AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File type {extension} is not allowed");
        }

        var maxSizeBytes = _uploadSettings.MaxFileSizeMB * 1024 * 1024;
        if (file.Length > maxSizeBytes)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {_uploadSettings.MaxFileSizeMB}MB");
        }
    }

    private string CalculateMD5(Stream stream)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static FileInfoResponse MapToFileInfoResponse(FileRecord record)
    {
        return new FileInfoResponse
        {
            Id = record.Id,
            FileToken = record.FileToken,
            FolderToken = record.FolderToken,
            VersionToken = record.VersionToken,
            FileName = record.FileName,
            FileSize = record.FileSize,
            MimeType = record.MimeType,
            FileMD5 = record.FileMD5,
            UploadTime = record.UploadTime,
            IsDeleted = record.IsDeleted
        };
    }
}
