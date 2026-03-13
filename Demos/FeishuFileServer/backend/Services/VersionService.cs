using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mud.Feishu;
using Mud.Feishu.Interfaces;
using FeishuFileServer.Configuration;
using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services.Feishu;

namespace FeishuFileServer.Services;

public interface IVersionService
{
    Task<List<VersionResponse>> GetVersionsAsync(string fileToken, CancellationToken cancellationToken = default);
    Task<VersionCreateResponse> CreateVersionAsync(string fileToken, IFormFile file, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadVersionAsync(string fileToken, string versionToken, CancellationToken cancellationToken = default);
    Task RestoreVersionAsync(string fileToken, string versionToken, CancellationToken cancellationToken = default);
    Task DeleteVersionAsync(string fileToken, string versionToken, CancellationToken cancellationToken = default);
}

public class VersionService : IVersionService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly IFeishuDriveService _feishuDriveService;
    private readonly IFeishuTenantV1DriveFilesVersions _fileVersions;
    private readonly ILogger<VersionService> _logger;
    private readonly VersionManagementSettings _versionSettings;

    public VersionService(
        FeishuFileDbContext dbContext,
        IFeishuDriveService feishuDriveService,
        IFeishuTenantV1DriveFilesVersions fileVersions,
        IOptions<VersionManagementSettings> versionSettings,
        ILogger<VersionService> logger)
    {
        _dbContext = dbContext;
        _feishuDriveService = feishuDriveService;
        _fileVersions = fileVersions;
        _versionSettings = versionSettings.Value;
        _logger = logger;
    }

    public async Task<List<VersionResponse>> GetVersionsAsync(string fileToken, CancellationToken cancellationToken = default)
    {
        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"File with token {fileToken} not found");
        }

        var versions = await _dbContext.VersionRecords
            .Where(v => v.FileToken == fileToken)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

        return versions.Select(MapToVersionResponse).ToList();
    }

    public async Task<VersionCreateResponse> CreateVersionAsync(string fileToken, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (!_versionSettings.Enabled)
        {
            throw new InvalidOperationException("Version management is disabled");
        }

        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"File with token {fileToken} not found");
        }

        var existingVersions = await _dbContext.VersionRecords
            .Where(v => v.FileToken == fileToken)
            .ToListAsync(cancellationToken);

        if (existingVersions.Count >= _versionSettings.MaxVersionsPerFile)
        {
            var oldestVersion = existingVersions.OrderBy(v => v.VersionNumber).First();
            _dbContext.VersionRecords.Remove(oldestVersion);
        }

        foreach (var version in existingVersions)
        {
            version.IsCurrentVersion = false;
        }

        var newVersionNumber = existingVersions.Count > 0 
            ? existingVersions.Max(v => v.VersionNumber) + 1 
            : 1;

        var versionRecord = new VersionRecord
        {
            FileToken = fileToken,
            VersionToken = Guid.NewGuid().ToString("N"),
            VersionNumber = newVersionNumber,
            FileName = file.FileName,
            FileSize = file.Length,
            CreatedTime = DateTime.UtcNow,
            IsCurrentVersion = true
        };

        _dbContext.VersionRecords.Add(versionRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Version created successfully: {VersionToken}", versionRecord.VersionToken);

        return new VersionCreateResponse
        {
            VersionToken = versionRecord.VersionToken,
            VersionNumber = versionRecord.VersionNumber,
            CreatedTime = versionRecord.CreatedTime
        };
    }

    public async Task<byte[]> DownloadVersionAsync(string fileToken, string versionToken, CancellationToken cancellationToken = default)
    {
        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"File with token {fileToken} not found");
        }

        var versionRecord = await _dbContext.VersionRecords
            .FirstOrDefaultAsync(v => v.FileToken == fileToken && v.VersionToken == versionToken, cancellationToken);

        if (versionRecord == null)
        {
            throw new KeyNotFoundException($"Version {versionToken} not found for file {fileToken}");
        }

        try
        {
            var objType = GetObjectType(fileRecord.FileName);
            if (!string.IsNullOrEmpty(objType))
            {
                var result = await _fileVersions.GetFileVersionByFileTokenAsync(
                    fileToken, 
                    versionToken, 
                    objType, 
                    cancellationToken: cancellationToken);

                if (result?.Data != null && !string.IsNullOrEmpty(result.Data.VersionSuffix))
                {
                    _logger.LogInformation("Found version {VersionSuffix} for file {FileToken}", result.Data.VersionSuffix, fileToken);
                    return await _feishuDriveService.DownloadFileAsync(fileToken, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download version from Feishu, falling back to current file");
        }

        _logger.LogInformation("Downloading current file version for {FileToken}", fileToken);
        return await _feishuDriveService.DownloadFileAsync(fileToken, cancellationToken);
    }

    public async Task RestoreVersionAsync(string fileToken, string versionToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restoring version {VersionToken} for file {FileToken}", versionToken, fileToken);

        var existingVersions = await _dbContext.VersionRecords
            .Where(v => v.FileToken == fileToken)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingVersions)
        {
            existing.IsCurrentVersion = existing.VersionToken == versionToken;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Version restored successfully");
    }

    public async Task DeleteVersionAsync(string fileToken, string versionToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting version {VersionToken} for file {FileToken}", versionToken, fileToken);

        var versionRecord = await _dbContext.VersionRecords
            .FirstOrDefaultAsync(v => v.FileToken == fileToken && v.VersionToken == versionToken, cancellationToken);

        if (versionRecord != null)
        {
            if (versionRecord.IsCurrentVersion)
            {
                var otherVersion = await _dbContext.VersionRecords
                    .Where(v => v.FileToken == fileToken && v.VersionToken != versionToken)
                    .OrderByDescending(v => v.VersionNumber)
                    .FirstOrDefaultAsync(cancellationToken);

                if (otherVersion != null)
                {
                    otherVersion.IsCurrentVersion = true;
                }
            }

            _dbContext.VersionRecords.Remove(versionRecord);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Version deleted successfully");
    }

    private string? GetObjectType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".docx" => "docx",
            ".xlsx" => "sheet",
            _ => null
        };
    }

    private static VersionResponse MapToVersionResponse(VersionRecord record)
    {
        return new VersionResponse
        {
            VersionToken = record.VersionToken,
            VersionNumber = record.VersionNumber,
            FileName = record.FileName,
            FileSize = record.FileSize,
            CreatedTime = record.CreatedTime,
            IsCurrentVersion = record.IsCurrentVersion
        };
    }
}
