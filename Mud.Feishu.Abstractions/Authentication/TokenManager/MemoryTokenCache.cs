// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------


using Mud.Feishu.Abstractions;
using System.Collections.Concurrent;

namespace Mud.Feishu.TokenManager;

/// <summary>
/// 基于内存的令牌缓存实现
/// </summary>
/// <remarks>
/// 使用 ConcurrentDictionary 和 Lazy 实现线程安全的内存缓存。
/// 支持令牌过期检测、自动清理等功能，适用于单实例部署场景。
/// </remarks>
public class MemoryTokenCache : ITokenCache
{
    /// <summary>
    /// 缓存项数据结构
    /// </summary>
    /// <remarks>
    /// 存储令牌值和过期时间戳，用于判断令牌是否有效。
    /// </remarks>
    private class CacheEntry
    {
        public string Value { get; set; } = string.Empty;
        public long ExpirationTime { get; set; } // Unix时间戳（毫秒）
    }

    /// <summary>
    /// 缓存字典
    /// </summary>
    /// <remarks>
    /// 使用线程安全的 ConcurrentDictionary 存储缓存项。
    /// </remarks>
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    /// <summary>
    /// 日志记录器
    /// </summary>
    private readonly ILogger<MemoryTokenCache> _logger;

    /// <summary>
    /// 令牌刷新阈值时间（秒）
    /// </summary>
    /// <remarks>
    /// 在令牌过期前5分钟（300秒）开始刷新，避免因网络延迟等原因导致令牌失效。
    /// </remarks>
    private readonly int _refreshThresholdSeconds;

    /// <summary>
    /// 初始化 MemoryTokenCache 实例
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="refreshThresholdSeconds">令牌刷新阈值(秒),默认300秒</param>
    /// <exception cref="ArgumentNullException">当任何必需参数为null时抛出</exception>
    public MemoryTokenCache(ILogger<MemoryTokenCache> logger, int refreshThresholdSeconds = 300)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _refreshThresholdSeconds = refreshThresholdSeconds > 0 ? refreshThresholdSeconds : 300;
    }

    /// <summary>
    /// 从缓存中获取令牌
    /// </summary>
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (TryGetValidEntry(key, out var value))
        {
            _logger.LogDebug("Token cache hit for key: {Key}", key);
            return value;
        }

        _logger.LogDebug("Token cache miss for key: {Key}", key);
        return null;
    }

    /// <summary>
    /// 尝试从缓存中获取有效的缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">输出值，如果找到有效缓存项</param>
    /// <returns>如果找到有效的缓存项返回true，否则返回false</returns>
    /// <remarks>
    /// 内部公共方法，用于避免在 GetAsync 和 ExistsAsync 中重复缓存读取逻辑。
    /// </remarks>
    private bool TryGetValidEntry(string key, out string? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (_cache.TryGetValue(key, out var entry) && !IsExpired(entry))
        {
            value = entry.Value;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 将令牌存入缓存
    /// </summary>
    public async Task SetAsync(string key, string value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value));

        var entry = new CacheEntry
        {
            Value = value,
            ExpirationTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (long)expiration.TotalMilliseconds
        };

        _cache.AddOrUpdate(key, entry, (k, existing) => entry);
        _logger.LogDebug("Token cached with key: {Key}, expires in {Expiration} seconds",
            key, expiration.TotalSeconds);
    }

    /// <summary>
    /// 从缓存中移除令牌
    /// </summary>
    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var removed = _cache.TryRemove(key, out _);
        if (removed)
        {
            _logger.LogDebug("Token removed from cache for key: {Key}", key);
        }
        return removed;
    }

    /// <summary>
    /// 检查缓存中是否存在有效令牌
    /// </summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return TryGetValidEntry(key, out _);
    }

    /// <summary>
    /// 清理过期的令牌缓存
    /// </summary>
    public async Task CleanExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expiredKeys = _cache
            .Where(kvp => IsExpired(kvp.Value))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Cleaned {Count} expired tokens from memory cache", expiredKeys.Count);
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public async Task<(int Total, int Expired)> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var total = _cache.Count;
        var expired = _cache.Count(kvp => IsExpired(kvp.Value));
        return (total, expired);
    }

    /// <summary>
    /// 判断缓存项是否过期（考虑刷新阈值）
    /// </summary>
    /// <param name="entry">缓存项</param>
    /// <returns>如果已过期或即将过期返回true</returns>
    /// <remarks>
    /// 考虑令牌刷新阈值，提前在过期前一段时间就认为令牌无效。
    /// 逻辑：实际过期时间 - 刷新阈值 &lt; 当前时间，则认为令牌已过期。
    /// </remarks>
    private bool IsExpired(CacheEntry entry)
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var refreshThresholdMs = TimeSpan.FromSeconds(_refreshThresholdSeconds).TotalMilliseconds;
        // 实际过期时间减去刷新阈值，如果小于当前时间则认为已过期
        var effectiveExpirationTime = entry.ExpirationTime - (long)refreshThresholdMs;
        return effectiveExpirationTime < currentTime;
    }

}
