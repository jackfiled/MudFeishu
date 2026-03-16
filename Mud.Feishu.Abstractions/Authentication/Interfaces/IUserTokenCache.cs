// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.TokenManager;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 用户令牌缓存接口
/// </summary>
/// <remarks>
/// 专门用于存储用户令牌信息的缓存接口，支持存储完整的 UserTokenInfo 对象。
/// 与 ITokenCache 不同，此接口专门处理用户令牌的复杂存储需求。
/// </remarks>
public interface IUserTokenCache
{
    /// <summary>
    /// 获取用户令牌信息
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户令牌信息，如果不存在则返回null</returns>
    Task<UserTokenInfo?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 存储用户令牌信息
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="tokenInfo">用户令牌信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作是否成功</returns>
    Task SetAsync(string key, UserTokenInfo tokenInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除用户令牌缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作是否成功</returns>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查是否存在有效的用户令牌
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果存在有效令牌返回true</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期的用户令牌
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task CleanExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含总令牌数和过期令牌数的元组</returns>
    Task<(int Total, int Expired)> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
