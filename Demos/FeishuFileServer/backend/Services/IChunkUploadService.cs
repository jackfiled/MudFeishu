using FeishuFileServer.Models.DTOs;

namespace FeishuFileServer.Services;

/// <summary>
/// 分片上传服务接口
/// </summary>
public interface IChunkUploadService
{
    /// <summary>
    /// 初始化分片上传
    /// </summary>
    Task<InitChunkUploadResponse> InitUploadAsync(InitChunkUploadRequest request, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传分片
    /// </summary>
    Task<ChunkUploadResponse> UploadChunkAsync(string uploadId, int chunkNumber, Stream chunkData, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 完成分片上传
    /// </summary>
    Task<ChunkUploadResponse> CompleteUploadAsync(string uploadId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消分片上传
    /// </summary>
    Task CancelUploadAsync(string uploadId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取上传进度
    /// </summary>
    Task<ChunkUploadResponse?> GetProgressAsync(string uploadId, int userId, CancellationToken cancellationToken = default);
}
