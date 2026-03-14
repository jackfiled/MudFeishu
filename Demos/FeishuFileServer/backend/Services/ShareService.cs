using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services.Feishu;
using Microsoft.EntityFrameworkCore;

namespace FeishuFileServer.Services;

/// <summary>
/// 文件分享服务实现
/// 提供文件和文件夹的分享功能
/// </summary>
public class ShareService : IShareService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly IFeishuDriveService _feishuDriveService;
    private readonly IFileService _fileService;
    private readonly IFolderService _folderService;
    private readonly ILogger<ShareService> _logger;

    /// <summary>
    /// 初始化分享服务实例
    /// </summary>
    public ShareService(
        FeishuFileDbContext dbContext,
        IFeishuDriveService feishuDriveService,
        IFileService fileService,
        IFolderService folderService,
        ILogger<ShareService> logger)
    {
        _dbContext = dbContext;
        _feishuDriveService = feishuDriveService;
        _fileService = fileService;
        _folderService = folderService;
        _logger = logger;
    }

    /// <summary>
    /// 创建分享链接
    /// </summary>
    public async Task<ShareResponse> CreateShareAsync(CreateShareRequest request, int userId, CancellationToken cancellationToken = default)
    {
        string resourceName;
        if (request.ResourceType.Equals("File", StringComparison.OrdinalIgnoreCase))
        {
            var file = await _dbContext.FileRecords
                .FirstOrDefaultAsync(f => f.FileToken == request.ResourceToken && !f.IsDeleted, cancellationToken);
            if (file == null)
            {
                throw new KeyNotFoundException("文件不存在");
            }
            resourceName = file.FileName;
        }
        else if (request.ResourceType.Equals("Folder", StringComparison.OrdinalIgnoreCase))
        {
            var folder = await _dbContext.FolderRecords
                .FirstOrDefaultAsync(f => f.FolderToken == request.ResourceToken && !f.IsDeleted, cancellationToken);
            if (folder == null)
            {
                throw new KeyNotFoundException("文件夹不存在");
            }
            resourceName = folder.FolderName;
        }
        else
        {
            throw new ArgumentException("无效的资源类型");
        }

        var shareCode = GenerateShareCode();
        var share = new ShareRecord
        {
            ShareCode = shareCode,
            ResourceType = request.ResourceType,
            ResourceToken = request.ResourceToken,
            ResourceName = resourceName,
            CreatorId = userId,
            Password = string.IsNullOrEmpty(request.Password) ? null : BCrypt.Net.BCrypt.HashPassword(request.Password),
            ExpireTime = request.ExpireTime,
            MaxAccessCount = request.MaxAccessCount,
            AllowDownload = request.AllowDownload,
            CreatedTime = DateTime.UtcNow,
            IsActive = true
        };

        _dbContext.ShareRecords.Add(share);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Share created: {ShareCode} by user {UserId}", shareCode, userId);

        return MapToShareResponse(share);
    }

    /// <summary>
    /// 访问分享内容
    /// </summary>
    public async Task<ShareContentResponse> AccessShareAsync(string shareCode, string? password = null, CancellationToken cancellationToken = default)
    {
        var share = await _dbContext.ShareRecords
            .FirstOrDefaultAsync(s => s.ShareCode == shareCode && s.IsActive, cancellationToken);

        if (share == null)
        {
            throw new KeyNotFoundException("分享不存在或已失效");
        }

        ValidateShareAccess(share, password);

        share.AccessCount++;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new ShareContentResponse
        {
            ResourceType = share.ResourceType,
            ResourceName = share.ResourceName,
            AllowDownload = share.AllowDownload
        };

        if (share.ResourceType.Equals("File", StringComparison.OrdinalIgnoreCase))
        {
            response.File = await _fileService.GetFileAsync(share.ResourceToken, cancellationToken);
        }
        else if (share.ResourceType.Equals("Folder", StringComparison.OrdinalIgnoreCase))
        {
            response.FolderContents = await _folderService.GetFolderContentsAsync(share.ResourceToken, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// 获取用户的分享列表
    /// </summary>
    public async Task<ShareListResponse> GetUserSharesAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ShareRecords.Where(s => s.CreatorId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var shares = await query
            .OrderByDescending(s => s.CreatedTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new ShareListResponse
        {
            Shares = shares.Select(MapToShareResponse).ToList(),
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// 删除分享
    /// </summary>
    public async Task DeleteShareAsync(long shareId, int userId, CancellationToken cancellationToken = default)
    {
        var share = await _dbContext.ShareRecords
            .FirstOrDefaultAsync(s => s.Id == shareId && s.CreatorId == userId, cancellationToken);

        if (share == null)
        {
            throw new KeyNotFoundException("分享不存在或无权删除");
        }

        _dbContext.ShareRecords.Remove(share);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Share deleted: {ShareId} by user {UserId}", shareId, userId);
    }

    /// <summary>
    /// 更新分享设置
    /// </summary>
    public async Task<ShareResponse> UpdateShareAsync(long shareId, UpdateShareRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var share = await _dbContext.ShareRecords
            .FirstOrDefaultAsync(s => s.Id == shareId && s.CreatorId == userId, cancellationToken);

        if (share == null)
        {
            throw new KeyNotFoundException("分享不存在或无权修改");
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            share.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        share.ExpireTime = request.ExpireTime;
        share.MaxAccessCount = request.MaxAccessCount;
        share.AllowDownload = request.AllowDownload;
        share.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Share updated: {ShareId} by user {UserId}", shareId, userId);

        return MapToShareResponse(share);
    }

    /// <summary>
    /// 下载分享的文件
    /// </summary>
    public async Task<byte[]> DownloadSharedFileAsync(string shareCode, string? password = null, CancellationToken cancellationToken = default)
    {
        var share = await _dbContext.ShareRecords
            .FirstOrDefaultAsync(s => s.ShareCode == shareCode && s.IsActive, cancellationToken);

        if (share == null)
        {
            throw new KeyNotFoundException("分享不存在或已失效");
        }

        if (!share.AllowDownload)
        {
            throw new InvalidOperationException("该分享不允许下载");
        }

        ValidateShareAccess(share, password);

        share.AccessCount++;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!share.ResourceType.Equals("File", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("只能下载文件类型的分享");
        }

        return await _fileService.DownloadFileAsync(share.ResourceToken, null, cancellationToken);
    }

    /// <summary>
    /// 验证分享访问权限
    /// </summary>
    private void ValidateShareAccess(ShareRecord share, string? password)
    {
        if (share.ExpireTime.HasValue && share.ExpireTime.Value < DateTime.UtcNow)
        {
            throw new InvalidOperationException("分享已过期");
        }

        if (share.MaxAccessCount.HasValue && share.AccessCount >= share.MaxAccessCount.Value)
        {
            throw new InvalidOperationException("分享访问次数已达上限");
        }

        if (!string.IsNullOrEmpty(share.Password))
        {
            if (string.IsNullOrEmpty(password) || !BCrypt.Net.BCrypt.Verify(password, share.Password))
            {
                throw new UnauthorizedAccessException("访问密码错误");
            }
        }
    }

    /// <summary>
    /// 生成分享码
    /// </summary>
    private static string GenerateShareCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// 映射到分享响应DTO
    /// </summary>
    private static ShareResponse MapToShareResponse(ShareRecord share)
    {
        return new ShareResponse
        {
            Id = share.Id,
            ShareCode = share.ShareCode,
            ShareLink = $"/share/{share.ShareCode}",
            ResourceType = share.ResourceType,
            ResourceName = share.ResourceName,
            RequirePassword = !string.IsNullOrEmpty(share.Password),
            ExpireTime = share.ExpireTime,
            AllowDownload = share.AllowDownload,
            CreatedTime = share.CreatedTime
        };
    }
}
