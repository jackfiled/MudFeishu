using System.Diagnostics;
using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services.Feishu;
using Microsoft.EntityFrameworkCore;
using Mud.Feishu;
using Mud.Feishu.Interfaces;

using FileInfo = Mud.Feishu.DataModels.Drive.Folder.FileInfo;

namespace FeishuFileServer.Services;

public class FeishuSyncService : IFeishuSyncService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly IFeishuTenantV1DriveFolder _driveFolder;
    private readonly ILogger<FeishuSyncService> _logger;
    private static readonly Dictionary<int, SyncStatus> _syncStatuses = new();

    public FeishuSyncService(
        FeishuFileDbContext dbContext,
        IFeishuTenantV1DriveFolder driveFolder,
        ILogger<FeishuSyncService> logger)
    {
        _dbContext = dbContext;
        _driveFolder = driveFolder;
        _logger = logger;
    }

    public async Task<SyncResult> SyncAllAsync(int userId, CancellationToken cancellationToken = default)
    {
        var result = new SyncResult
        {
            SyncTime = DateTime.UtcNow
        };

        try
        {
            UpdateSyncStatus(userId, true, 0, "开始同步...");

            var rootFolders = await SyncFolderRecursiveAsync(null, userId, result, cancellationToken);

            result.Success = true;
            _logger.LogInformation("同步完成: 新增文件夹 {AddedFolders}, 更新文件夹 {UpdatedFolders}, 新增文件 {AddedFiles}, 更新文件 {UpdatedFiles}",
                result.AddedFolders, result.UpdatedFolders, result.AddedFiles, result.UpdatedFiles);

            UpdateSyncStatus(userId, false, 100, "同步完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步失败");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            UpdateSyncStatus(userId, false, 0, $"同步失败: {ex.Message}");
        }

        return result;
    }

    public async Task<SyncResult> SyncFolderAsync(string folderToken, int userId, CancellationToken cancellationToken = default)
    {
        var result = new SyncResult
        {
            SyncTime = DateTime.UtcNow
        };

        try
        {
            UpdateSyncStatus(userId, true, 0, $"同步文件夹 {folderToken}...");

            await SyncFolderRecursiveAsync(folderToken, userId, result, cancellationToken);

            result.Success = true;

            UpdateSyncStatus(userId, false, 100, "同步完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步文件夹失败");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            UpdateSyncStatus(userId, false, 0, $"同步失败: {ex.Message}");
        }

        return result;
    }

    public Task<SyncStatus> GetSyncStatusAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (_syncStatuses.TryGetValue(userId, out var status))
        {
            return Task.FromResult(status);
        }

        return Task.FromResult(new SyncStatus
        {
            IsSyncing = false,
            Progress = 0
        });
    }

    private async Task<List<FolderRecord>> SyncFolderRecursiveAsync(string? folderToken, int userId, SyncResult result, CancellationToken cancellationToken)
    {
        var folders = new List<FolderRecord>();
        string? pageToken = null;

        do
        {
            var response = await _driveFolder.GetFilesPageListAsync(
                folderToken,
                page_token: pageToken,
                cancellationToken: cancellationToken);

            if (response?.Data?.Files == null || response.Data.Files.Length == 0)
            {
                break;
            }

            foreach (var file in response.Data.Files)
            {
                if (file.Type == "folder")
                {
                    var folder = await SyncFolderRecordAsync(file, userId, result, cancellationToken);
                    if (folder != null)
                    {
                        folders.Add(folder);
                        result.SyncedFolders++;

                        var subFolders = await SyncFolderRecursiveAsync(file.Token, userId, result, cancellationToken);
                        foreach (var subFolder in subFolders)
                        {
                            subFolder.ParentFolderToken = folder.FolderToken;
                        }
                    }
                }
                else
                {
                    await SyncFileRecordAsync(file, folderToken, userId, result, cancellationToken);
                    result.SyncedFiles++;
                }
            }

            pageToken = response.Data.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return folders;
    }

    private async Task<FolderRecord?> SyncFolderRecordAsync(Mud.Feishu.DataModels.Drive.Folder.FileInfo file, int userId, SyncResult result, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(file.Token))
        {
            return null;
        }

        var existingFolder = await _dbContext.FolderRecords
            .FirstOrDefaultAsync(f => f.FolderToken == file.Token, cancellationToken);

        if (existingFolder == null)
        {
            var newFolder = new FolderRecord
            {
                FolderToken = file.Token,
                FolderName = file.Name ?? "未命名文件夹",
                ParentFolderToken = file.ParentToken,
                CreatedTime = ParseDateTime(file.CreatedTime) ?? DateTime.UtcNow,
                IsDeleted = false
            };

            _dbContext.FolderRecords.Add(newFolder);
            result.AddedFolders++;

            _logger.LogDebug("新增文件夹: {FolderName} ({FolderToken})", newFolder.FolderName, newFolder.FolderToken);
            return newFolder;
        }
        else
        {
            if (existingFolder.FolderName != file.Name)
            {
                existingFolder.FolderName = file.Name ?? existingFolder.FolderName;
                existingFolder.CreatedTime = ParseDateTime(file.CreatedTime) ?? existingFolder.CreatedTime;
                result.UpdatedFolders++;

                _logger.LogDebug("更新文件夹: {FolderName} ({FolderToken})", existingFolder.FolderName, existingFolder.FolderToken);
            }
            return existingFolder;
        }
    }

    private async Task SyncFileRecordAsync(Mud.Feishu.DataModels.Drive.Folder.FileInfo file, string? folderToken, int userId, SyncResult result, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(file.Token))
        {
            return;
        }

        var existingFile = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == file.Token, cancellationToken);

        if (existingFile == null)
        {
            var newFile = new FileRecord
            {
                FileToken = file.Token,
                FileName = file.Name ?? "未命名文件",
                FileSize = 0,
                MimeType = file.Type ?? "application/octet-stream",
                FolderToken = folderToken,
                UploadTime = ParseDateTime(file.CreatedTime) ?? DateTime.UtcNow,
                UserId = userId,
                IsDeleted = false
            };

            _dbContext.FileRecords.Add(newFile);
            result.AddedFiles++;

            _logger.LogDebug("新增文件: {FileName} ({FileToken})", newFile.FileName, newFile.FileToken);
        }
        else
        {
            var needUpdate = false;

            if (existingFile.FileName != file.Name)
            {
                existingFile.FileName = file.Name ?? existingFile.FileName;
                needUpdate = true;
            }

            if (needUpdate)
            {
                result.UpdatedFiles++;
                _logger.LogDebug("更新文件: {FileName} ({FileToken})", existingFile.FileName, existingFile.FileToken);
            }
        }
    }

    private void UpdateSyncStatus(int userId, bool isSyncing, int progress, string currentItem)
    {
        var status = new SyncStatus
        {
            IsSyncing = isSyncing,
            Progress = progress,
            CurrentItem = currentItem,
            LastSyncTime = DateTime.UtcNow
        };

        if (_syncStatuses.ContainsKey(userId))
        {
            _syncStatuses[userId] = status;
        }
        else
        {
            _syncStatuses.Add(userId, status);
        }
    }

    private static DateTime? ParseDateTime(string? timeStr)
    {
        if (string.IsNullOrEmpty(timeStr))
        {
            return null;
        }

        if (long.TryParse(timeStr, out var timestamp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }

        if (DateTime.TryParse(timeStr, out var dt))
        {
            return dt;
        }

        return null;
    }
}
