// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Services;

/// <summary>
/// 基于内存的分布式事件去重服务实现
/// 适用于单机部署或开发测试环境
/// 对于分布式部署，建议使用 Redis 等外部存储实现
/// </summary>
public class FeishuEventDistributedDeduplicator : IFeishuEventDistributedDeduplicator, IAsyncDisposable
{
    private readonly ILogger<FeishuEventDistributedDeduplicator>? _logger;
    private readonly Dictionary<string, EventCacheEntry> _eventCache;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _cacheExpiration;
    private readonly TimeSpan _cleanupInterval;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    public FeishuEventDistributedDeduplicator(
        ILogger<FeishuEventDistributedDeduplicator>? logger = null,
        TimeSpan? cacheExpiration = null,
        TimeSpan? cleanupInterval = null)
    {
        _logger = logger;
        _eventCache = new Dictionary<string, EventCacheEntry>();
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromHours(24); // 默认缓存24小时
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(5); // 默认每5分钟清理一次

        // 启动定期清理任务
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, _cleanupInterval, _cleanupInterval);

        _logger?.LogInformation("飞书分布式事件去重服务初始化完成，缓存过期时间: {Expiration}, 清理间隔: {CleanupInterval}",
            _cacheExpiration, _cleanupInterval);
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkAsProcessedAsync(string eventId, string? appKey = null, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        if (string.IsNullOrEmpty(eventId))
        {
            _logger?.LogWarning("事件ID为空，跳过去重检查");
            return false;
        }

        var cacheKey = GetCacheKey(eventId, appKey);

        lock (_lock)
        {
            // 先清理已过期的条目
            CleanupExpiredEntriesLocked();

            // 检查是否已存在且未过期
            if (_eventCache.TryGetValue(cacheKey, out var entry))
            {
                var actualTtl = ttl ?? _cacheExpiration;
                if (DateTimeOffset.UtcNow - entry.ProcessedAt <= actualTtl)
                {
                    _logger?.LogDebug("事件 {EventId} (AppKey: {AppKey}) 已处理过，跳过", eventId, appKey ?? "default");
                    return true; // 已处理
                }
                // 已过期，删除并继续处理
                _eventCache.Remove(cacheKey);
            }

            // 记录新事件
            _eventCache[cacheKey] = new EventCacheEntry
            {
                ProcessedAt = DateTimeOffset.UtcNow,
                EventId = eventId,
                AppKey = appKey
            };

            _logger?.LogDebug("事件 {EventId} (AppKey: {AppKey}) 标记为已处理", eventId, appKey ?? "default");
            return false; // 未处理，新事件
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsProcessedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        if (string.IsNullOrEmpty(eventId))
        {
            return false;
        }

        var cacheKey = GetCacheKey(eventId, appKey);

        lock (_lock)
        {
            if (!_eventCache.TryGetValue(cacheKey, out var entry))
                return false;

            // 检查是否已过期
            return (DateTimeOffset.UtcNow - entry.ProcessedAt) <= _cacheExpiration;
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            return CleanupExpiredEntriesLocked();
        }
    }

    /// <summary>
    /// 清理过期条目（内部方法，需要在锁内调用）
    /// </summary>
    private int CleanupExpiredEntriesLocked()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredKeys = _eventCache
            .Where(kvp => (now - kvp.Value.ProcessedAt) > _cacheExpiration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _eventCache.Remove(key);
        }

        if (expiredKeys.Count > 0)
        {
            _logger?.LogDebug("清理了 {Count} 个过期的事件缓存条目", expiredKeys.Count);
        }

        return expiredKeys.Count;
    }

    /// <summary>
    /// 定期清理过期条目
    /// </summary>
    private void CleanupExpiredEntries(object? state)
    {
        lock (_lock)
        {
            CleanupExpiredEntriesLocked();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cleanupTimer.Dispose();

        lock (_lock)
        {
            _eventCache.Clear();
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 事件缓存条目
    /// </summary>
    private class EventCacheEntry
    {
        public string EventId { get; set; } = string.Empty;
        public string? AppKey { get; set; }
        public DateTimeOffset ProcessedAt { get; set; }
    }

    /// <summary>
    /// 生成缓存键
    /// </summary>
    /// <param name="eventId">事件ID</param>
    /// <param name="appKey">应用键（可选）</param>
    /// <returns>缓存键</returns>
    private static string GetCacheKey(string eventId, string? appKey = null)
    {
        // 如果提供了 appKey，将其包含在键中以实现多应用隔离
        // 格式: {appKey}:{eventId} 或 {eventId}
        if (!string.IsNullOrEmpty(appKey))
        {
            return $"{appKey}:{eventId}";
        }
        return eventId;
    }
}
