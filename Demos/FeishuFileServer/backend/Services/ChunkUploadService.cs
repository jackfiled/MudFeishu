using System.Security.Cryptography;
using System.Text.Json;
using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services.Feishu;
using Microsoft.EntityFrameworkCore;

namespace FeishuFileServer.Services;

/// <summary>
/// 分片上传服务实现
/// 支持大文件分片上传到飞书云空间
/// </summary>
public class ChunkUploadService : IChunkUploadService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly IFeishuDriveService _feishuDriveService;
    private readonly ILogger<ChunkUploadService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _uploadDirectory;
    private static readonly HashSet<string> _activeUploads = new();
    private static readonly object _lockObject = new();

    public ChunkUploadService(
        FeishuFileDbContext dbContext,
        IFeishuDriveService feishuDriveService,
        ILogger<ChunkUploadService> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _feishuDriveService = feishuDriveService;
        _logger = logger;
        _configuration = configuration;
        _uploadDirectory = configuration["FileUploadSettings:UploadDirectory"] ?? "uploads";

        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }
    }

    private static bool TryAcquireUploadLock(string uploadId)
    {
        lock (_lockObject)
        {
            return _activeUploads.Add(uploadId);
        }
    }

    private static void ReleaseUploadLock(string uploadId)
    {
        lock (_lockObject)
        {
            _activeUploads.Remove(uploadId);
        }
    }

    private static bool IsUploadLocked(string uploadId)
    {
        lock (_lockObject)
        {
            return _activeUploads.Contains(uploadId);
        }
    }

    public async Task<InitChunkUploadResponse> InitUploadAsync(InitChunkUploadRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var chunkSize = request.ChunkSize > 0 ? request.ChunkSize : 4 * 1024 * 1024;
        var totalChunks = (int)Math.Ceiling((double)request.FileSize / chunkSize);
        var uploadId = Guid.NewGuid().ToString("N");

        if (!TryAcquireUploadLock(uploadId))
        {
            throw new InvalidOperationException("无法获取上传锁，请稍后重试");
        }

        try
        {
            string? feishuUploadId = null;
            try
            {
                feishuUploadId = await _feishuDriveService.InitChunkUploadAsync(
                    request.FileName,
                    request.FileSize,
                    request.FolderToken,
                    cancellationToken);

                _logger.LogInformation("飞书分片上传初始化成功: FeishuUploadId={FeishuUploadId}", feishuUploadId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "飞书分片上传初始化失败，将使用本地存储");
            }

            var record = new ChunkUploadRecord
            {
                UploadId = uploadId,
                FileName = request.FileName,
                FileSize = request.FileSize,
                FileMD5 = request.FileMD5,
                ChunkSize = chunkSize,
                TotalChunks = totalChunks,
                UploadedChunks = 0,
                UploadedChunkNumbers = JsonSerializer.Serialize(new Dictionary<int, bool>()),
                FolderToken = request.FolderToken,
                UserId = userId,
                CreatedTime = DateTime.UtcNow,
                IsCompleted = false,
                IsCancelled = false
            };

            _dbContext.ChunkUploadRecords.Add(record);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var tempDir = Path.Combine(_uploadDirectory, "chunks", uploadId);
            Directory.CreateDirectory(tempDir);

            _logger.LogInformation("初始化分片上传: UploadId={UploadId}, FileName={FileName}, TotalChunks={TotalChunks}",
                uploadId, request.FileName, totalChunks);

            return new InitChunkUploadResponse
            {
                UploadId = uploadId,
                FileName = request.FileName,
                FileSize = request.FileSize,
                ChunkSize = chunkSize,
                TotalChunks = totalChunks
            };
        }
        catch
        {
            ReleaseUploadLock(uploadId);
            throw;
        }
    }

    public async Task<ChunkUploadResponse> UploadChunkAsync(string uploadId, int chunkNumber, Stream chunkData, int userId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ChunkUploadRecords
            .FirstOrDefaultAsync(r => r.UploadId == uploadId && r.UserId == userId, cancellationToken);

        if (record == null)
        {
            throw new InvalidOperationException("上传任务不存在");
        }

        if (record.IsCompleted)
        {
            throw new InvalidOperationException("上传任务已完成");
        }

        if (record.IsCancelled)
        {
            throw new InvalidOperationException("上传任务已取消");
        }

        if (chunkNumber < 0 || chunkNumber >= record.TotalChunks)
        {
            throw new InvalidOperationException($"分片序号无效，有效范围: 0-{record.TotalChunks - 1}");
        }

        var tempDir = Path.Combine(_uploadDirectory, "chunks", uploadId);
        var chunkFilePath = Path.Combine(tempDir, $"chunk_{chunkNumber}");

        using (var fileStream = File.Create(chunkFilePath))
        {
            await chunkData.CopyToAsync(fileStream, cancellationToken);
        }

        var uploadedNumbers = JsonSerializer.Deserialize<Dictionary<int, bool>>(record.UploadedChunkNumbers ?? "{}") ?? new Dictionary<int, bool>();
        if (!uploadedNumbers.ContainsKey(chunkNumber))
        {
            uploadedNumbers[chunkNumber] = true;
            record.UploadedChunkNumbers = JsonSerializer.Serialize(uploadedNumbers);
            record.UploadedChunks = uploadedNumbers.Count;
            record.UpdatedTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var progress = (double)record.UploadedChunks / record.TotalChunks * 100;

        _logger.LogInformation("上传分片: UploadId={UploadId}, ChunkNumber={ChunkNumber}, Progress={Progress}%",
            uploadId, chunkNumber, progress);

        return new ChunkUploadResponse
        {
            UploadId = uploadId,
            ChunkNumber = chunkNumber,
            IsComplete = false,
            UploadedChunks = record.UploadedChunks,
            TotalChunks = record.TotalChunks,
            Progress = Math.Round(progress, 2)
        };
    }

    public async Task<ChunkUploadResponse> CompleteUploadAsync(string uploadId, int userId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ChunkUploadRecords
            .FirstOrDefaultAsync(r => r.UploadId == uploadId && r.UserId == userId, cancellationToken);

        if (record == null)
        {
            throw new InvalidOperationException("上传任务不存在");
        }

        if (record.UploadedChunks < record.TotalChunks)
        {
            throw new InvalidOperationException($"分片未上传完成，已上传: {record.UploadedChunks}/{record.TotalChunks}");
        }

        var tempDir = Path.Combine(_uploadDirectory, "chunks", uploadId);
        string? fileToken = null;

        try
        {
            var feishuUploadId = await _feishuDriveService.InitChunkUploadAsync(
                record.FileName,
                record.FileSize,
                record.FolderToken,
                cancellationToken);

            _logger.LogInformation("开始上传分片到飞书云空间: FeishuUploadId={FeishuUploadId}", feishuUploadId);

            for (int i = 0; i < record.TotalChunks; i++)
            {
                var chunkFilePath = Path.Combine(tempDir, $"chunk_{i}");
                if (File.Exists(chunkFilePath))
                {
                    var chunkData = await File.ReadAllBytesAsync(chunkFilePath, cancellationToken);
                    await _feishuDriveService.UploadChunkAsync(feishuUploadId, i, chunkData, cancellationToken);
                    _logger.LogInformation("飞书分片上传进度: {Current}/{Total}", i + 1, record.TotalChunks);
                }
            }

            fileToken = await _feishuDriveService.CompleteChunkUploadAsync(feishuUploadId, record.TotalChunks, cancellationToken);
            _logger.LogInformation("飞书云空间上传完成: FileToken={FileToken}", fileToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传到飞书云空间失败，使用本地存储");

            var finalFilePath = Path.Combine(_uploadDirectory, record.FileName);
            await using var finalStream = File.Create(finalFilePath);
            for (int i = 0; i < record.TotalChunks; i++)
            {
                var chunkFilePath = Path.Combine(tempDir, $"chunk_{i}");
                if (File.Exists(chunkFilePath))
                {
                    await using var chunkStream = File.OpenRead(chunkFilePath);
                    await chunkStream.CopyToAsync(finalStream, cancellationToken);
                }
            }
            fileToken = Guid.NewGuid().ToString("N");
        }

        var fileRecord = new FileRecord
        {
            FileToken = fileToken ?? Guid.NewGuid().ToString("N"),
            FolderToken = record.FolderToken,
            FileName = record.FileName,
            FileSize = record.FileSize,
            MimeType = GetMimeType(record.FileName),
            FileMD5 = record.FileMD5,
            UploadTime = DateTime.UtcNow,
            UserId = userId
        };

        _dbContext.FileRecords.Add(fileRecord);

        record.IsCompleted = true;
        record.FileToken = fileToken;
        record.UpdatedTime = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理临时目录失败: {TempDir}", tempDir);
        }

        _logger.LogInformation("分片上传完成: UploadId={UploadId}, FileToken={FileToken}", uploadId, fileToken);

        ReleaseUploadLock(uploadId);

        return new ChunkUploadResponse
        {
            UploadId = uploadId,
            ChunkNumber = record.TotalChunks - 1,
            IsComplete = true,
            UploadedChunks = record.TotalChunks,
            TotalChunks = record.TotalChunks,
            Progress = 100,
            FileToken = fileToken
        };
    }

    public async Task CancelUploadAsync(string uploadId, int userId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ChunkUploadRecords
            .FirstOrDefaultAsync(r => r.UploadId == uploadId && r.UserId == userId, cancellationToken);

        if (record == null)
        {
            return;
        }

        record.IsCancelled = true;
        record.UpdatedTime = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        ReleaseUploadLock(uploadId);

        var tempDir = Path.Combine(_uploadDirectory, "chunks", uploadId);
        try
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "清理临时目录失败: {TempDir}", tempDir);
        }

        _logger.LogInformation("取消分片上传: UploadId={UploadId}", uploadId);
    }

    public async Task<ChunkUploadResponse?> GetProgressAsync(string uploadId, int userId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ChunkUploadRecords
            .FirstOrDefaultAsync(r => r.UploadId == uploadId && r.UserId == userId, cancellationToken);

        if (record == null)
        {
            return null;
        }

        var progress = (double)record.UploadedChunks / record.TotalChunks * 100;

        return new ChunkUploadResponse
        {
            UploadId = uploadId,
            ChunkNumber = record.UploadedChunks,
            IsComplete = record.IsCompleted,
            UploadedChunks = record.UploadedChunks,
            TotalChunks = record.TotalChunks,
            Progress = Math.Round(progress, 2),
            FileToken = record.FileToken
        };
    }

    private static string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".tiff" => "image/tiff",
            _ => "application/octet-stream"
        };
    }
}
