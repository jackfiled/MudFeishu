using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mud.Feishu;
using Mud.Feishu.Interfaces;
using Mud.Feishu.DataModels.Drive.FileVersions;
using Mud.Feishu.DataModels.Drive.Files;
using FeishuFileServer.Configuration;
using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;

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
    private readonly ILogger<VersionService> _logger;
    private readonly VersionManagementSettings _versionSettings;

    public VersionService(
        FeishuFileDbContext dbContext,
        IOptions<VersionManagementSettings> versionSettings,
        ILogger<VersionService> logger)
    {
        _dbContext = dbContext;
        _versionSettings = versionSettings.Value;
        _logger = logger;
    }

    public async Task<List<VersionResponse>> GetVersionsAsync(string fileToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting versions for file {FileToken}", fileToken);

        var versions = await _dbContext.VersionRecords
            .Where(v => v.FileToken == fileToken)
            .OrderByDescending(v => v.CreatedTime)
            .ToListAsync(cancellationToken);

        return versions.Select(v => new VersionResponse
        {
            FileToken = v.FileToken,
            VersionToken = v.VersionToken,
            VersionNumber = v.VersionNumber,
            FileName = v.FileName,
            FileSize = v.FileSize,
            CreatedTime = v.CreatedTime,
            IsCurrentVersion = v.IsCurrentVersion
        }).ToList();
    }

    public async Task<VersionCreateResponse> CreateVersionAsync(string fileToken, IFormFile file, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new version for file {FileToken}", fileToken);

        var existingVersions = await _dbContext.VersionRecords
            .Where(v => v.FileToken == fileToken)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingVersions)
        {
            existing.IsCurrentVersion = false;
        }

        var newVersionNumber = existingVersions.Count > 0 ? existingVersions.Max(v => v.VersionNumber) + 1 : 1;

        var versionRecord = new VersionRecord
        {
            FileToken = fileToken,
            VersionToken = Guid.NewGuid().ToString(),
            VersionNumber = newVersionNumber,
            FileName = file.FileName,
            FileSize = file.Length,
            FileMD5 = string.Empty,
            CreatedTime = DateTime.UtcNow,
            IsCurrentVersion = true
        };

        _dbContext.VersionRecords.Add(versionRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_versionSettings.Enabled && _versionSettings.MaxVersionsPerFile > 0)
        {
            var oldVersions = await _dbContext.VersionRecords
                .Where(v => v.FileToken == fileToken && !v.IsCurrentVersion)
                .OrderByDescending(v => v.CreatedTime)
                .Skip(_versionSettings.MaxVersionsPerFile)
                .ToListAsync(cancellationToken);

            if (oldVersions.Any())
            {
                _dbContext.VersionRecords.RemoveRange(oldVersions);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

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
        _logger.LogInformation("Downloading version {VersionToken} for file {FileToken}", versionToken, fileToken);

        var versionRecord = await _dbContext.VersionRecords
            .FirstOrDefaultAsync(v => v.FileToken == fileToken && v.VersionToken == versionToken, cancellationToken);

        if (versionRecord == null)
        {
            throw new KeyNotFoundException($"Version {versionToken} not found for file {fileToken}");
        }

        throw new NotImplementedException("Version download requires Feishu API integration");
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
            _dbContext.VersionRecords.Remove(versionRecord);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Version deleted successfully");
    }
}
