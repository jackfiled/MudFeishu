// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;

namespace Mud.Feishu.TokenManager;

/// <summary>
/// 基于内存的用户令牌缓存实现
/// </summary>
/// <remarks>
/// 使用 ConcurrentDictionary 实现线程安全的内存缓存，专门存储 UserTokenInfo 对象。
/// 支持访问令牌和刷新令牌的过期检测。
/// </remarks>
public class MemoryUserTokenCache : MemoryCacheBase<UserTokenInfo>, IUserTokenCache
{
    /// <summary>
    /// 初始化 MemoryUserTokenCache 实例
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="refreshThresholdSeconds">令牌刷新阈值(秒)，默认300秒</param>
    public MemoryUserTokenCache(ILogger<MemoryUserTokenCache> logger, int refreshThresholdSeconds = 300)
        : base(logger, refreshThresholdSeconds)
    {
    }

    /// <summary>
    /// 判断缓存项是否有效
    /// </summary>
    /// <param name="entry">缓存项</param>
    /// <returns>如果访问令牌有效返回 true</returns>
    protected override bool IsValid(UserTokenInfo entry)
    {
        return entry.IsAccessTokenValid(_refreshThresholdSeconds);
    }

    /// <summary>
    /// 记录缓存命中日志
    /// </summary>
    /// <param name="key">缓存键</param>
    protected override void LogCacheHit(string key)
    {
        _logger.LogDebug("User token cache hit for key: {Key}", key);
    }

    /// <summary>
    /// 记录缓存未命中日志
    /// </summary>
    /// <param name="key">缓存键</param>
    protected override void LogCacheMiss(string key)
    {
        _logger.LogDebug("User token cache miss for key: {Key}", key);
    }

    /// <summary>
    /// 当缓存项被添加时调用
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="entry">被添加的缓存项</param>
    protected override void OnEntryAdded(string key, UserTokenInfo entry)
    {
        _logger.LogDebug("User token cached with key: {Key}, userId: {UserId}", key, entry.UserId);
    }

    /// <summary>
    /// 当缓存项被移除时调用
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="entry">被移除的缓存项</param>
    protected override void OnEntryRemoved(string key, UserTokenInfo entry)
    {
        _logger.LogDebug("User token removed from cache for key: {Key}", key);
    }

    /// <summary>
    /// 获取用户令牌信息
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户令牌信息，如果不存在或访问令牌已过期则返回 null</returns>
    public Task<UserTokenInfo?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var entry = GetCore(key);
        if (entry == null)
        {
            _logger.LogDebug("User token expired for key: {Key}", key);
        }
        return Task.FromResult(entry);
    }

    /// <summary>
    /// 存储用户令牌信息
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="tokenInfo">用户令牌信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    public Task SetAsync(string key, UserTokenInfo tokenInfo, CancellationToken cancellationToken = default)
    {
        SetCore(key, tokenInfo);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 移除用户令牌缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功移除</returns>
    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RemoveCore(key));
    }

    /// <summary>
    /// 检查是否存在有效的用户令牌
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果存在有效令牌返回 true</returns>
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ExistsCore(key));
    }

    /// <summary>
    /// 清理过期的用户令牌
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    public Task CleanExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expiredKeys = _cache
            .Where(kvp => !kvp.Value.IsAccessTokenValid(_refreshThresholdSeconds) && !kvp.Value.IsRefreshTokenValid())
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Cleaned {Count} expired user tokens from memory cache", expiredKeys.Count);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含总数和过期数的元组</returns>
    public Task<(int Total, int Expired)> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(GetStatisticsCore());
    }

    /// <summary>
    /// 获取用户令牌信息（即使访问令牌已过期，只要刷新令牌有效也返回）
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户令牌信息，如果不存在或刷新令牌也过期则返回 null</returns>
    public Task<UserTokenInfo?> GetWithRefreshTokenAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Task.FromResult<UserTokenInfo?>(null);

        if (_cache.TryGetValue(key, out var tokenInfo) && tokenInfo.IsRefreshTokenValid())
        {
            return Task.FromResult<UserTokenInfo?>(tokenInfo);
        }

        return Task.FromResult<UserTokenInfo?>(null);
    }
}
