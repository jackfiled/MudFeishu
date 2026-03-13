// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services.Feishu;
using Microsoft.EntityFrameworkCore;
using Mud.Feishu;
using Mud.Feishu.DataModels.Drive.Folder;

namespace FeishuFileServer.Services;

public interface IFolderService
{
    Task<FolderResponse> CreateFolderAsync(FolderCreateRequest request, int? userId = null, CancellationToken cancellationToken = default);
    Task<FolderResponse> UpdateFolderAsync(string folderToken, FolderUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteFolderAsync(string folderToken, CancellationToken cancellationToken = default);
    Task<FolderResponse?> GetFolderAsync(string folderToken, CancellationToken cancellationToken = default);
    Task<FolderListResponse> GetFoldersAsync(string? parentFolderToken = null, int? userId = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<FolderContentsResponse> GetFolderContentsAsync(string folderToken, CancellationToken cancellationToken = default);
}

public class FolderService : IFolderService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly IFeishuTenantV1DriveFolder _driveFolder;
    private readonly IFeishuDriveService _feishuDriveService;
    private readonly ILogger<FolderService> _logger;

    public FolderService(
        FeishuFileDbContext dbContext,
        IFeishuTenantV1DriveFolder driveFolder,
        IFeishuDriveService feishuDriveService,
        ILogger<FolderService> logger)
    {
        _dbContext = dbContext;
        _driveFolder = driveFolder;
        _feishuDriveService = feishuDriveService;
        _logger = logger;
    }

    public async Task<FolderResponse> CreateFolderAsync(FolderCreateRequest request, int? userId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating folder {FolderName} in parent {ParentFolderToken} by user {UserId}", request.Name, request.ParentFolderToken, userId);

        var createRequest = new CreateFolderRequest
        {
            Name = request.Name,
            FolderToken = request.ParentFolderToken ?? string.Empty
        };

        var result = await _driveFolder.CreateFolderAsync(createRequest, cancellationToken);

        if (result?.Data == null)
        {
            throw new Exception($"Failed to create folder: {result?.Msg}");
        }

        var folderRecord = new FolderRecord
        {
            FolderToken = result.Data.Token,
            FolderName = request.Name,
            ParentFolderToken = request.ParentFolderToken,
            CreatedTime = DateTime.UtcNow,
            UserId = userId
        };

        _dbContext.FolderRecords.Add(folderRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Folder created successfully: {FolderToken}", folderRecord.FolderToken);

        return MapToFolderResponse(folderRecord);
    }

    public async Task<FolderResponse> UpdateFolderAsync(string folderToken, FolderUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var folderRecord = await _dbContext.FolderRecords
            .FirstOrDefaultAsync(f => f.FolderToken == folderToken && !f.IsDeleted, cancellationToken);

        if (folderRecord == null)
        {
            throw new KeyNotFoundException($"Folder with token {folderToken} not found");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            folderRecord.FolderName = request.Name;
        }

        if (!string.IsNullOrEmpty(request.ParentFolderToken) && request.ParentFolderToken != folderRecord.ParentFolderToken)
        {
            folderRecord.ParentFolderToken = request.ParentFolderToken;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Folder updated: {FolderToken}", folderToken);

        return MapToFolderResponse(folderRecord);
    }

    public async Task DeleteFolderAsync(string folderToken, CancellationToken cancellationToken = default)
    {
        var folderRecord = await _dbContext.FolderRecords
            .FirstOrDefaultAsync(f => f.FolderToken == folderToken && !f.IsDeleted, cancellationToken);

        if (folderRecord == null)
        {
            throw new KeyNotFoundException($"Folder with token {folderToken} not found");
        }

        try
        {
            await _feishuDriveService.DeleteFolderAsync(folderToken, cancellationToken);
            _logger.LogInformation("Folder deleted from Feishu: {FolderToken}", folderToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete folder from Feishu, continuing with local deletion: {FolderToken}", folderToken);
        }

        var subFolders = await _dbContext.FolderRecords
            .Where(f => f.ParentFolderToken == folderToken && !f.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var subFolder in subFolders)
        {
            subFolder.IsDeleted = true;
        }

        var files = await _dbContext.FileRecords
            .Where(f => f.FolderToken == folderToken && !f.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var file in files)
        {
            file.IsDeleted = true;
        }

        folderRecord.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Folder deleted locally: {FolderToken}", folderToken);
    }

    public async Task<FolderResponse?> GetFolderAsync(string folderToken, CancellationToken cancellationToken = default)
    {
        var folderRecord = await _dbContext.FolderRecords
            .FirstOrDefaultAsync(f => f.FolderToken == folderToken && !f.IsDeleted, cancellationToken);

        if (folderRecord == null)
        {
            return null;
        }

        return MapToFolderResponse(folderRecord);
    }

    public async Task<FolderListResponse> GetFoldersAsync(string? parentFolderToken = null, int? userId = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FolderRecords.Where(f => !f.IsDeleted);

        if (!string.IsNullOrEmpty(parentFolderToken))
        {
            query = query.Where(f => f.ParentFolderToken == parentFolderToken);
        }

        if (userId.HasValue)
        {
            query = query.Where(f => f.UserId == userId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var folders = await query
            .OrderByDescending(f => f.CreatedTime)
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

    public async Task<FolderContentsResponse> GetFolderContentsAsync(string folderToken, CancellationToken cancellationToken = default)
    {
        var response = new FolderContentsResponse
        {
            Folders = new List<FolderResponse>(),
            Files = new List<FileInfoResponse>()
        };

        var folderRecord = await _dbContext.FolderRecords
            .FirstOrDefaultAsync(f => f.FolderToken == folderToken && !f.IsDeleted, cancellationToken);

        if (folderRecord != null)
        {
            var subFolders = await _dbContext.FolderRecords
                .Where(f => f.ParentFolderToken == folderToken && !f.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var subFolder in subFolders)
            {
                response.Folders.Add(MapToFolderResponse(subFolder));
            }

            var files = await _dbContext.FileRecords
                .Where(f => f.FolderToken == folderToken && !f.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var file in files)
            {
                response.Files.Add(new FileInfoResponse
                {
                    FileToken = file.FileToken,
                    FolderToken = file.FolderToken,
                    FileName = file.FileName,
                    MimeType = file.MimeType,
                    UploadTime = file.UploadTime
                });
            }
        }

        return response;
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
