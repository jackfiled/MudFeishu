// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 令牌缓存抽象接口
/// </summary>
/// <remarks>
/// 提供令牌缓存的抽象层，支持多种缓存实现（内存、Redis等）。
/// 使用依赖注入可以灵活切换不同的缓存实现。
/// </remarks>
public interface ITokenCache
{
    /// <summary>
    /// 从缓存中获取令牌
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存的令牌字符串，如果不存在则返回null</returns>
    /// <remarks>
    /// 根据缓存键获取已存储的令牌，如果令牌不存在或已过期则返回null。
    /// </remarks>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 将令牌存入缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">令牌字符串</param>
    /// <param name="expiration">过期时间间隔</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作是否成功</returns>
    /// <remarks>
    /// 将令牌存入缓存并设置过期时间。
    /// 如果缓存中已存在相同键的令牌，则覆盖旧值。
    /// </remarks>
    Task SetAsync(string key, string value, TimeSpan expiration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从缓存中移除令牌
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作是否成功（如果键不存在则返回false）</returns>
    /// <remarks>
    /// 移除指定键的令牌缓存，通常用于令牌失效或用户登出场景。
    /// </remarks>
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查缓存中是否存在有效令牌
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果存在有效令牌返回true，否则返回false</returns>
    /// <remarks>
    /// 快速检查指定键是否存在有效令牌，不返回令牌内容。
    /// </remarks>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期的令牌缓存
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <remarks>
    /// 清理所有已过期的令牌缓存，释放存储空间。
    /// 某些缓存实现（如Redis）会自动清理过期数据，此方法可能为空实现。
    /// </remarks>
    Task CleanExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含总令牌数和过期令牌数的元组</returns>
    /// <remarks>
    /// 返回缓存的统计信息，用于监控和调优：
    /// - Total: 缓存中的令牌总数
    /// - Expired: 已过期或即将过期的令牌数量
    /// </remarks>
    Task<(int Total, int Expired)> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
