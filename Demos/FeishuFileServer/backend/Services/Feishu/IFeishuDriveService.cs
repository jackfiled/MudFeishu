using FeishuFileServer.Models;

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
    /// 初始化分片上传到飞书云盘
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <param name="fileSize">文件大小</param>
    /// <param name="folderToken">目标文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>飞书上传事务ID</returns>
    Task<string> InitChunkUploadAsync(string fileName, long fileSize, string? folderToken = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传分片到飞书云盘
    /// </summary>
    /// <param name="uploadId">飞书上传事务ID</param>
    /// <param name="seq">分片序号</param>
    /// <param name="chunkData">分片数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UploadChunkAsync(string uploadId, int seq, byte[] chunkData, CancellationToken cancellationToken = default);

    /// <summary>
    /// 完成分片上传到飞书云盘
    /// </summary>
    /// <param name="uploadId">飞书上传事务ID</param>
    /// <param name="totalChunks">总分片数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件令牌</returns>
    Task<string> CompleteChunkUploadAsync(string uploadId, int totalChunks, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// 获取文件夹内的文件列表
    /// </summary>
    /// <param name="folderToken">文件夹令牌，为空表示根目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件记录列表</returns>
    Task<List<FileRecord>> GetFilesAsync(string? folderToken = null, CancellationToken cancellationToken = default);
}
