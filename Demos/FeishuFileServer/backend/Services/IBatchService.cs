using FeishuFileServer.Models.DTOs;

namespace FeishuFileServer.Services;

/// <summary>
/// 批量操作服务接口
/// 提供批量删除、移动、复制功能
/// </summary>
public interface IBatchService
{
    /// <summary>
    /// 批量删除文件和文件夹
    /// </summary>
    /// <param name="request">批量删除请求</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量操作响应</returns>
    Task<BatchOperationResponse> BatchDeleteAsync(BatchDeleteRequest request, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量移动文件和文件夹
    /// </summary>
    /// <param name="request">批量移动请求</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量操作响应</returns>
    Task<BatchOperationResponse> BatchMoveAsync(BatchMoveRequest request, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量复制文件和文件夹
    /// </summary>
    /// <param name="request">批量复制请求</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量操作响应</returns>
    Task<BatchOperationResponse> BatchCopyAsync(BatchCopyRequest request, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量恢复文件和文件夹
    /// </summary>
    /// <param name="request">批量恢复请求</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量操作响应</returns>
    Task<BatchOperationResponse> BatchRestoreAsync(BatchRestoreRequest request, int userId, CancellationToken cancellationToken = default);
}
