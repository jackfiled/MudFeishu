// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 带应用键前缀的令牌缓存
/// </summary>
/// <remarks>
/// 包装原始的令牌缓存实现，为每个缓存键自动添加应用前缀。
/// 确保不同应用的令牌缓存互不干扰，实现缓存隔离。
/// 
/// 例如：
/// - 应用 "app1" 的缓存键 "tenant:token" 会被转换为 "app1:tenant:token"
/// - 应用 "app2" 的缓存键 "tenant:token" 会被转换为 "app2:tenant:token"
/// </remarks>
internal class PrefixedTokenCache : ITokenCache
{
    /// <summary>
    /// 基础令牌缓存
    /// </summary>
    /// <remarks>
    /// 实际存储令牌的底层缓存实现。
    /// </remarks>
    private readonly ITokenCache _baseCache;

    /// <summary>
    /// 应用前缀
    /// </summary>
    /// <remarks>
    /// 用于区分不同应用的缓存键前缀。
    /// </remarks>
    private readonly string _prefix;

    /// <summary>
    /// 初始化带前缀的令牌缓存
    /// </summary>
    /// <param name="baseCache">基础令牌缓存</param>
    /// <param name="prefix">应用前缀</param>
    /// <exception cref="ArgumentNullException">当任何参数为null时抛出</exception>
    public PrefixedTokenCache(ITokenCache baseCache, string prefix)
    {
        _baseCache = baseCache ?? throw new ArgumentNullException(nameof(baseCache));
        _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
    }

    /// <summary>
    /// 为缓存键添加前缀
    /// </summary>
    /// <param name="key">原始缓存键</param>
    /// <returns>添加前缀后的缓存键</returns>
    private string PrefixKey(string key) => $"{_prefix}:{key}";

    /// <summary>
    /// 从缓存中获取令牌
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>缓存的令牌字符串，如果不存在则返回null</returns>
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        return _baseCache.GetAsync(PrefixKey(key), cancellationToken);
    }

    /// <summary>
    /// 将令牌存入缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">令牌字符串</param>
    /// <param name="expiration">过期时间间隔</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作是否成功</returns>
    public Task SetAsync(string key, string value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        return _baseCache.SetAsync(PrefixKey(key), value, expiration, cancellationToken);
    }

    /// <summary>
    /// 从缓存中移除令牌
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作是否成功（如果键不存在则返回false）</returns>
    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return _baseCache.RemoveAsync(PrefixKey(key), cancellationToken);
    }

    /// <summary>
    /// 检查缓存中是否存在有效令牌
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果存在有效令牌返回true，否则返回false</returns>
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return _baseCache.ExistsAsync(PrefixKey(key), cancellationToken);
    }

    /// <summary>
    /// 清理过期的令牌缓存
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <remarks>
    /// 清理所有已过期的令牌缓存，释放存储空间。
    /// 某些缓存实现（如Redis）会自动清理过期数据，此方法可能为空实现。
    /// </remarks>
    public Task CleanExpiredAsync(CancellationToken cancellationToken = default)
    {
        return _baseCache.CleanExpiredAsync(cancellationToken);
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含总令牌数和过期令牌数的元组</returns>
    /// <remarks>
    /// 注意：此方法返回的是基础缓存的统计信息，包含了所有应用的缓存数据。
    /// 如果需要获取单个应用的统计信息，需要在基础缓存实现中添加相应功能。
    /// </remarks>
    public Task<(int Total, int Expired)> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return _baseCache.GetStatisticsAsync(cancellationToken);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    /// <remarks>
    /// 清理缓存占用的资源。
    /// </remarks>
    public void Dispose()
    {
        if (_baseCache is IDisposable disposable)
            disposable.Dispose();
    }
}
