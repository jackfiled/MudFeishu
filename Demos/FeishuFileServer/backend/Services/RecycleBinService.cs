using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services.Feishu;
using Microsoft.EntityFrameworkCore;

namespace FeishuFileServer.Services;

/// <summary>
/// 回收站服务实现
/// 提供回收站文件的查询、恢复和永久删除功能
/// </summary>
public class RecycleBinService : IRecycleBinService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly IFeishuDriveService _feishuDriveService;
    private readonly ILogger<RecycleBinService> _logger;

    /// <summary>
    /// 初始化回收站服务实例
    /// </summary>
    public RecycleBinService(
        FeishuFileDbContext dbContext,
        IFeishuDriveService feishuDriveService,
        ILogger<RecycleBinService> logger)
    {
        _dbContext = dbContext;
        _feishuDriveService = feishuDriveService;
        _logger = logger;
    }

    /// <summary>
    /// 获取回收站中的文件列表
    /// </summary>
    public async Task<FileListResponse> GetDeletedFilesAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FileRecords
            .Where(f => f.UserId == userId && f.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var files = await query
            .OrderByDescending(f => f.DeletedTime)
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

    /// <summary>
    /// 获取回收站中的文件夹列表
    /// </summary>
    public async Task<FolderListResponse> GetDeletedFoldersAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FolderRecords
            .Where(f => f.UserId == userId && f.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var folders = await query
            .OrderByDescending(f => f.DeletedTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new FolderListResponse
        {
            Folders = folders.Select(MapToFolderResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 恢复文件
    /// </summary>
    public async Task RestoreFileAsync(string fileToken, CancellationToken cancellationToken = default)
    {
        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"文件 {fileToken} 不存在或不在回收站中");
        }

        fileRecord.IsDeleted = false;
        fileRecord.DeletedTime = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("文件已恢复: {FileToken}", fileToken);
    }

    /// <summary>
    /// 恢复文件夹
    /// </summary>
    public async Task RestoreFolderAsync(string folderToken, CancellationToken cancellationToken = default)
    {
        var folderRecord = await _dbContext.FolderRecords
            .FirstOrDefaultAsync(f => f.FolderToken == folderToken && f.IsDeleted, cancellationToken);

        if (folderRecord == null)
        {
            throw new KeyNotFoundException($"文件夹 {folderToken} 不存在或不在回收站中");
        }

        folderRecord.IsDeleted = false;
        folderRecord.DeletedTime = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("文件夹已恢复: {FolderToken}", folderToken);
    }

    /// <summary>
    /// 永久删除文件
    /// </summary>
    public async Task PermanentlyDeleteFileAsync(string fileToken, CancellationToken cancellationToken = default)
    {
        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"文件 {fileToken} 不存在或不在回收站中");
        }

        try
        {
            await _feishuDriveService.DeleteFileAsync(fileToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从飞书删除文件失败: {FileToken}", fileToken);
        }

        var versionRecords = await _dbContext.VersionRecords
            .Where(v => v.FileToken == fileToken)
            .ToListAsync(cancellationToken);
        _dbContext.VersionRecords.RemoveRange(versionRecords);

        _dbContext.FileRecords.Remove(fileRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("文件已永久删除: {FileToken}", fileToken);
    }

    /// <summary>
    /// 永久删除文件夹
    /// </summary>
    public async Task PermanentlyDeleteFolderAsync(string folderToken, CancellationToken cancellationToken = default)
    {
        var folderRecord = await _dbContext.FolderRecords
            .FirstOrDefaultAsync(f => f.FolderToken == folderToken && f.IsDeleted, cancellationToken);

        if (folderRecord == null)
        {
            throw new KeyNotFoundException($"文件夹 {folderToken} 不存在或不在回收站中");
        }

        try
        {
            await _feishuDriveService.DeleteFolderAsync(folderToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从飞书删除文件夹失败: {FolderToken}", folderToken);
        }

        _dbContext.FolderRecords.Remove(folderRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("文件夹已永久删除: {FolderToken}", folderToken);
    }

    /// <summary>
    /// 清空回收站
    /// </summary>
    public async Task<int> EmptyRecycleBinAsync(int userId, CancellationToken cancellationToken = default)
    {
        var deletedFiles = await _dbContext.FileRecords
            .Where(f => f.UserId == userId && f.IsDeleted)
            .ToListAsync(cancellationToken);

        var deletedFolders = await _dbContext.FolderRecords
            .Where(f => f.UserId == userId && f.IsDeleted)
            .ToListAsync(cancellationToken);

        var totalDeleted = deletedFiles.Count + deletedFolders.Count;

        foreach (var file in deletedFiles)
        {
            try
            {
                await _feishuDriveService.DeleteFileAsync(file.FileToken, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "从飞书删除文件失败: {FileToken}", file.FileToken);
            }
        }

        foreach (var folder in deletedFolders)
        {
            try
            {
                await _feishuDriveService.DeleteFolderAsync(folder.FolderToken, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "从飞书删除文件夹失败: {FolderToken}", folder.FolderToken);
            }
        }

        var fileTokens = deletedFiles.Select(f => f.FileToken).ToList();
        var versionRecords = await _dbContext.VersionRecords
            .Where(v => fileTokens.Contains(v.FileToken))
            .ToListAsync(cancellationToken);
        _dbContext.VersionRecords.RemoveRange(versionRecords);

        _dbContext.FileRecords.RemoveRange(deletedFiles);
        _dbContext.FolderRecords.RemoveRange(deletedFolders);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("回收站已清空，共删除 {Count} 项", totalDeleted);

        return totalDeleted;
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

    private static FolderResponse MapToFolderResponse(FolderRecord record)
    {
        return new FolderResponse
        {
            Id = record.Id,
            FolderToken = record.FolderToken,
            FolderName = record.FolderName,
            ParentFolderToken = record.ParentFolderToken,
            CreatedTime = record.CreatedTime,
            IsDeleted = record.IsDeleted
        };
    }
}
