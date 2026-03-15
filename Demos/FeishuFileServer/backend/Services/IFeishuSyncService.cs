using FeishuFileServer.Models.DTOs;

namespace FeishuFileServer.Services;

/// <summary>
/// 飞书云空间同步服务接口
/// </summary>
public interface IFeishuSyncService
{
    /// <summary>
    /// 同步飞书云空间的所有文件夹和文件
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步结果</returns>
    Task<SyncResult> SyncAllAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 同步指定文件夹
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步结果</returns>
    Task<SyncResult> SyncFolderAsync(string folderToken, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取同步状态
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步状态</returns>
    Task<SyncStatus> GetSyncStatusAsync(int userId, CancellationToken cancellationToken = default);
}
