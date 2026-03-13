// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 通用内存去重服务实现
/// 支持多种键类型（string、ulong 等），适用于事件去重、Nonce 去重、SeqID 去重等场景
/// </summary>
/// <typeparam name="TKey">键类型</typeparam>
public class MemoryDeduplicator<TKey> : IAsyncDisposable where TKey : notnull
{
    private readonly ILogger<MemoryDeduplicator<TKey>>? _logger;
    private readonly Dictionary<TKey, CacheEntry> _cache;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _cacheExpiration;
    private readonly TimeSpan _cleanupInterval;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="cacheExpiration">缓存过期时间，默认 24 小时</param>
    /// <param name="cleanupInterval">清理间隔时间，默认 5 分钟</param>
    public MemoryDeduplicator(
        ILogger<MemoryDeduplicator<TKey>>? logger = null,
        TimeSpan? cacheExpiration = null,
        TimeSpan? cleanupInterval = null)
    {
        _logger = logger;
        _cache = new Dictionary<TKey, CacheEntry>();
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromHours(24);
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(5);

        _cleanupTimer = new Timer(CleanupExpiredEntries, null, _cleanupInterval, _cleanupInterval);

        _logger?.LogInformation("内存去重服务初始化完成，类型: {KeyType}, 缓存过期时间: {Expiration}, 清理间隔: {CleanupInterval}",
            typeof(TKey).Name, _cacheExpiration, _cleanupInterval);
    }

    /// <summary>
    /// 尝试将键标记为已处理
    /// </summary>
    /// <param name="key">键值</param>
    /// <param name="appKey">应用键（用于多应用场景）</param>
    /// <returns>如果已处理过返回 true，否则返回 false 并标记</returns>
    public bool TryMarkAsProcessed(TKey key, string? appKey = null)
    {
        if (key == null)
        {
            _logger?.LogWarning("键为空，跳过去重检查");
            return false;
        }

        var cacheKey = GetCacheKey(key, appKey);

        lock (_lock)
        {
            CleanupExpiredEntriesLocked();

            if (_cache.TryGetValue(cacheKey, out var entry))
            {
                if (DateTimeOffset.UtcNow - entry.ProcessedAt <= _cacheExpiration)
                {
                    _logger?.LogDebug("键 {Key} (AppKey: {AppKey}) 已处理过，跳过", key, appKey ?? "default");
                    return true;
                }
                _cache.Remove(cacheKey);
            }

            _cache[cacheKey] = new CacheEntry
            {
                Key = key,
                ProcessedAt = DateTimeOffset.UtcNow,
                AppKey = appKey
            };

            _logger?.LogDebug("键 {Key} (AppKey: {AppKey}) 标记为已处理", key, appKey ?? "default");
            return false;
        }
    }

    /// <summary>
    /// 检查键是否已处理
    /// </summary>
    /// <param name="key">键值</param>
    /// <param name="appKey">应用键</param>
    /// <returns>如果已处理返回 true</returns>
    public bool IsProcessed(TKey key, string? appKey = null)
    {
        if (key == null)
            return false;

        var cacheKey = GetCacheKey(key, appKey);

        lock (_lock)
        {
            if (!_cache.TryGetValue(cacheKey, out var entry))
                return false;

            return (DateTimeOffset.UtcNow - entry.ProcessedAt) <= _cacheExpiration;
        }
    }

    /// <summary>
    /// 清理过期条目
    /// </summary>
    /// <returns>清理的条目数量</returns>
    public int CleanupExpired()
    {
        lock (_lock)
        {
            return CleanupExpiredEntriesLocked();
        }
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public virtual void ClearCache()
    {
        lock (_lock)
        {
            var count = _cache.Count;
            _cache.Clear();
            _logger?.LogInformation("清空了 {Count} 个缓存条目", count);
        }
    }

    /// <summary>
    /// 获取缓存统计
    /// </summary>
    /// <returns>总缓存数和过期数</returns>
    public (int Total, int Expired) GetCacheStats()
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            var expiredCount = _cache.Values.Count(e =>
                (now - e.ProcessedAt) > _cacheExpiration);

            return (_cache.Count, expiredCount);
        }
    }

    /// <summary>
    /// 获取缓存数量
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _cache.Count;
            }
        }
    }

    /// <summary>
    /// 获取缓存过期时间
    /// </summary>
    protected TimeSpan CacheExpiration => _cacheExpiration;

    /// <summary>
    /// 获取日志记录器（供派生类使用）
    /// </summary>
    protected ILogger? Logger => _logger;

    /// <summary>
    /// 获取锁对象（供派生类使用）
    /// </summary>
    protected object Lock => _lock;

    /// <summary>
    /// 获取缓存字典（供派生类使用）
    /// </summary>
    protected Dictionary<TKey, CacheEntry> Cache => _cache;

    private int CleanupExpiredEntriesLocked()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => (now - kvp.Value.ProcessedAt) > _cacheExpiration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.Remove(key);
        }

        if (expiredKeys.Count > 0)
        {
            _logger?.LogDebug("清理了 {Count} 个过期缓存条目", expiredKeys.Count);
        }

        return expiredKeys.Count;
    }

    private void CleanupExpiredEntries(object? state)
    {
        lock (_lock)
        {
            CleanupExpiredEntriesLocked();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
            return new ValueTask();

        _disposed = true;
        _cleanupTimer.Dispose();

        lock (_lock)
        {
            _cache.Clear();
        }

        return new ValueTask();
    }

    private static TKey GetCacheKey(TKey key, string? appKey)
    {
        // 对于值类型，无法组合键，直接返回原键
        if (typeof(TKey).IsValueType || string.IsNullOrEmpty(appKey))
            return key;

        // 对于字符串类型，可以组合 appKey
        if (key is string strKey)
        {
            return (TKey)(object)$"{appKey}:{strKey}";
        }

        return key;
    }

    /// <summary>
    /// 缓存条目
    /// </summary>
    protected class CacheEntry
    {
        /// <summary>
        /// 键值
        /// </summary>
        public TKey Key { get; set; } = default!;

        /// <summary>
        /// 应用键
        /// </summary>
        public string? AppKey { get; set; }

        /// <summary>
        /// 处理时间
        /// </summary>
        public DateTimeOffset ProcessedAt { get; set; }
    }
}


