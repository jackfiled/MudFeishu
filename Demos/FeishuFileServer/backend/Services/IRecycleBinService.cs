using FeishuFileServer.Models.DTOs;

namespace FeishuFileServer.Services;

/// <summary>
/// 回收站服务接口
/// 提供回收站文件的查询、恢复和永久删除功能
/// </summary>
public interface IRecycleBinService
{
    /// <summary>
    /// 获取回收站中的文件列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件列表响应</returns>
    Task<FileListResponse> GetDeletedFilesAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取回收站中的文件夹列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹列表响应</returns>
    Task<FolderListResponse> GetDeletedFoldersAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// 恢复文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RestoreFileAsync(string fileToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 恢复文件夹
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RestoreFolderAsync(string folderToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 永久删除文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task PermanentlyDeleteFileAsync(string fileToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 永久删除文件夹
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task PermanentlyDeleteFolderAsync(string folderToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清空回收站
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的项目数量</returns>
    Task<int> EmptyRecycleBinAsync(int userId, CancellationToken cancellationToken = default);
}
