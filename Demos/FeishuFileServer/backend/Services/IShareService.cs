using FeishuFileServer.Models.DTOs;

namespace FeishuFileServer.Services;

/// <summary>
/// 文件分享服务接口
/// 提供文件和文件夹的分享功能
/// </summary>
public interface IShareService
{
    /// <summary>
    /// 创建分享链接
    /// </summary>
    /// <param name="request">创建分享请求</param>
    /// <param name="userId">创建者用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分享响应</returns>
    Task<ShareResponse> CreateShareAsync(CreateShareRequest request, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 访问分享内容
    /// </summary>
    /// <param name="shareCode">分享码</param>
    /// <param name="password">访问密码（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分享内容响应</returns>
    Task<ShareContentResponse> AccessShareAsync(string shareCode, string? password = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的分享列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分享列表响应</returns>
    Task<ShareListResponse> GetUserSharesAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除分享
    /// </summary>
    /// <param name="shareId">分享ID</param>
    /// <param name="userId">用户ID（验证权限）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteShareAsync(long shareId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新分享设置
    /// </summary>
    /// <param name="shareId">分享ID</param>
    /// <param name="request">更新请求</param>
    /// <param name="userId">用户ID（验证权限）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的分享响应</returns>
    Task<ShareResponse> UpdateShareAsync(long shareId, UpdateShareRequest request, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载分享的文件
    /// </summary>
    /// <param name="shareCode">分享码</param>
    /// <param name="password">访问密码（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容字节数组</returns>
    Task<byte[]> DownloadSharedFileAsync(string shareCode, string? password = null, CancellationToken cancellationToken = default);
}
