// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// 带 Redis 降级策略的分布式事件去重服务
/// 当 Redis 连接失败时自动降级到内存去重，并支持指数退避重试
/// </summary>
/// <remarks>
/// 此实现提供高可用性保障：
/// 1. 正常情况使用 Redis 分布式去重
/// 2. Redis 失败时自动降级到内存去重
/// 3. 支持指数退避重试机制
/// 4. 记录降级和恢复事件
/// </remarks>
public class RedisFeishuEventDistributedDeduplicatorWithFallback : IFeishuEventDistributedDeduplicator, IAsyncDisposable
{
    private readonly ILogger<RedisFeishuEventDistributedDeduplicatorWithFallback>? _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly IFeishuEventDistributedDeduplicator _fallbackDeduplicator;
    private readonly TimeSpan _defaultCacheExpiration;
    private readonly string _keyPrefix;
    private readonly int _maxRetryCount;
    private readonly TimeSpan _initialRetryDelay;
    private readonly TimeSpan _maxRetryDelay;

    private bool _redisAvailable = true;
    private int _consecutiveFailures = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private readonly SemaphoreSlim _retrySemaphore = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// 获取当前是否使用 Redis
    /// </summary>
    public bool IsUsingRedis => _redisAvailable;

    /// <summary>
    /// 获取连续失败次数
    /// </summary>
    public int ConsecutiveFailures => _consecutiveFailures;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="redis">Redis 连接多路复用器</param>
    /// <param name="logger">日志记录器（可选）</param>
    /// <param name="cacheExpiration">默认缓存过期时间</param>
    /// <param name="keyPrefix">Redis 键前缀，默认为 "feishu:event:"</param>
    /// <param name="maxRetryCount">最大重试次数，默认为 3</param>
    /// <param name="initialRetryDelay">初始重试延迟，默认为 1 秒</param>
    /// <param name="maxRetryDelay">最大重试延迟，默认为 30 秒</param>
    public RedisFeishuEventDistributedDeduplicatorWithFallback(
        IConnectionMultiplexer redis,
        ILogger<RedisFeishuEventDistributedDeduplicatorWithFallback>? logger = null,
        TimeSpan? cacheExpiration = null,
        string? keyPrefix = null,
        int maxRetryCount = 3,
        TimeSpan? initialRetryDelay = null,
        TimeSpan? maxRetryDelay = null)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger;
        _database = _redis.GetDatabase();
        _defaultCacheExpiration = cacheExpiration ?? TimeSpan.FromHours(24);
        _keyPrefix = keyPrefix ?? "feishu:event:";
        _maxRetryCount = maxRetryCount;
        _initialRetryDelay = initialRetryDelay ?? TimeSpan.FromSeconds(1);
        _maxRetryDelay = maxRetryDelay ?? TimeSpan.FromSeconds(30);

        // 创建内存降级去重器
        _fallbackDeduplicator = new FeishuEventDistributedDeduplicator(logger as ILogger<FeishuEventDistributedDeduplicator>, _defaultCacheExpiration);

        _logger?.LogInformation("飞书 Redis 分布式事件去重服务（带降级）初始化完成，缓存过期时间: {Expiration}, 键前缀: {KeyPrefix}, 最大重试: {MaxRetry}",
            _defaultCacheExpiration, _keyPrefix, _maxRetryCount);
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkAsProcessedAsync(string eventId, string? appKey = null, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            _logger?.LogWarning("事件ID为空，跳过去重检查");
            return false;
        }

        // 如果 Redis 不可用，直接使用降级策略
        if (!_redisAvailable)
        {
            return await UseFallbackAsync(eventId, appKey, ttl, cancellationToken, "Redis 不可用");
        }

        // 尝试使用 Redis，带重试机制
        try
        {
            return await TryMarkWithRetryAsync(eventId, appKey, ttl, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return await HandleRedisFailureAsync(eventId, appKey, ttl, cancellationToken, ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsProcessedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return false;
        }

        // 如果 Redis 不可用，直接使用降级策略
        if (!_redisAvailable)
        {
            return await _fallbackDeduplicator.IsProcessedAsync(eventId, appKey, cancellationToken);
        }

        // 尝试使用 Redis
        try
        {
            return await CheckWithRetryAsync(eventId, appKey, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return await HandleRedisFailureForCheckAsync(eventId, appKey, cancellationToken, ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Redis 使用 EXPIRE 自动清理，只清理内存降级器
            return await _fallbackDeduplicator.CleanupExpiredAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "清理过期条目时发生错误");
            return 0;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        await _fallbackDeduplicator.DisposeAsync();
        _retrySemaphore.Dispose();

        // ConnectionMultiplexer 应由调用者管理，此处不释放
        await Task.CompletedTask;
    }

    /// <summary>
    /// 尝试使用 Redis 标记事件，带重试机制
    /// </summary>
    private async Task<bool> TryMarkWithRetryAsync(string eventId, string? appKey, TimeSpan? ttl, CancellationToken cancellationToken)
    {
        var actualTtl = ttl ?? _defaultCacheExpiration;
        var redisKey = GetRedisKey(eventId, appKey);

        for (int attempt = 0; attempt < _maxRetryCount; attempt++)
        {
            try
            {
                // 使用 SETNX + EXPIRE 实现原子性去重
                var setResult = await _database.StringSetAsync(
                    redisKey,
                    "1",
                    actualTtl,
                    When.NotExists);

                if (!setResult)
                {
                    _logger?.LogDebug("事件 {EventId} 已处理过，跳过", eventId);
                    return true; // 已处理
                }

                // 成功，重置失败计数
                ResetFailureCount();
                _logger?.LogDebug("事件 {EventId} 标记为已处理，TTL: {Ttl}", eventId, actualTtl);
                return false; // 未处理，新事件
            }
            catch (RedisConnectionException ex)
            {
                _logger?.LogWarning(ex, "Redis 连接失败 (尝试 {Attempt}/{MaxRetry})", attempt + 1, _maxRetryCount);

                // 最后一次尝试失败，抛出异常
                if (attempt == _maxRetryCount - 1)
                    throw;

                // 等待重试（指数退避）
                var delay = CalculateRetryDelay(attempt);
                await Task.Delay(delay, cancellationToken);
            }
            catch (RedisTimeoutException ex)
            {
                _logger?.LogWarning(ex, "Redis 超时 (尝试 {Attempt}/{MaxRetry})", attempt + 1, _maxRetryCount);

                if (attempt == _maxRetryCount - 1)
                    throw;

                var delay = CalculateRetryDelay(attempt);
                await Task.Delay(delay, cancellationToken);
            }
            catch (RedisException ex)
            {
                _logger?.LogError(ex, "Redis 错误 (尝试 {Attempt}/{MaxRetry})", attempt + 1, _maxRetryCount);
                throw;
            }
        }

        return false;
    }

    /// <summary>
    /// 尝试使用 Redis 检查事件，带重试机制
    /// </summary>
    private async Task<bool> CheckWithRetryAsync(string eventId, string? appKey, CancellationToken cancellationToken)
    {
        var redisKey = GetRedisKey(eventId, appKey);

        for (int attempt = 0; attempt < _maxRetryCount; attempt++)
        {
            try
            {
                var exists = await _database.KeyExistsAsync(redisKey);
                ResetFailureCount();
                _logger?.LogDebug("事件 {EventId} 处理状态: {Status}", eventId, exists ? "已处理" : "未处理");
                return exists;
            }
            catch (RedisConnectionException ex)
            {
                _logger?.LogWarning(ex, "Redis 连接失败 (尝试 {Attempt}/{MaxRetry})", attempt + 1, _maxRetryCount);

                if (attempt == _maxRetryCount - 1)
                    throw;

                var delay = CalculateRetryDelay(attempt);
                await Task.Delay(delay, cancellationToken);
            }
            catch (RedisTimeoutException ex)
            {
                _logger?.LogWarning(ex, "Redis 超时 (尝试 {Attempt}/{MaxRetry})", attempt + 1, _maxRetryCount);

                if (attempt == _maxRetryCount - 1)
                    throw;

                var delay = CalculateRetryDelay(attempt);
                await Task.Delay(delay, cancellationToken);
            }
        }

        return false;
    }

    /// <summary>
    /// 处理 Redis 失败，降级到内存去重
    /// </summary>
    private async Task<bool> HandleRedisFailureAsync(string eventId, string? appKey, TimeSpan? ttl, CancellationToken cancellationToken, Exception ex)
    {
        await _retrySemaphore.WaitAsync(cancellationToken);
        try
        {
            _consecutiveFailures++;
            _lastFailureTime = DateTime.UtcNow;

            _logger?.LogError(ex, "Redis 失败，连续失败次数: {FailCount}, 降级到内存去重", _consecutiveFailures);

            // 如果连续失败超过阈值，标记 Redis 不可用
            if (_consecutiveFailures >= 3)
            {
                _redisAvailable = false;
                _logger?.LogWarning("连续失败 {FailCount} 次，标记 Redis 为不可用，使用内存去重", _consecutiveFailures);
            }

            return await UseFallbackAsync(eventId, appKey, ttl, cancellationToken, "Redis 失败");
        }
        finally
        {
            _retrySemaphore.Release();
        }
    }

    /// <summary>
    /// 处理 Redis 失败（检查操作）
    /// </summary>
    private async Task<bool> HandleRedisFailureForCheckAsync(string eventId, string? appKey, CancellationToken cancellationToken, Exception ex)
    {
        await _retrySemaphore.WaitAsync(cancellationToken);
        try
        {
            _consecutiveFailures++;
            _lastFailureTime = DateTime.UtcNow;

            _logger?.LogError(ex, "Redis 失败（检查操作），连续失败次数: {FailCount}, 使用内存去重", _consecutiveFailures);

            if (_consecutiveFailures >= 3)
            {
                _redisAvailable = false;
                _logger?.LogWarning("连续失败 {FailCount} 次，标记 Redis 为不可用", _consecutiveFailures);
            }

            return await _fallbackDeduplicator.IsProcessedAsync(eventId, appKey, cancellationToken);
        }
        finally
        {
            _retrySemaphore.Release();
        }
    }

    /// <summary>
    /// 使用内存降级去重器
    /// </summary>
    private async Task<bool> UseFallbackAsync(string eventId, string? appKey, TimeSpan? ttl, CancellationToken cancellationToken, string reason)
    {
        _logger?.LogDebug("使用内存降级去重器，原因: {Reason}, 事件ID: {EventId}, AppKey: {AppKey}", reason, eventId, appKey ?? "default");
        return await _fallbackDeduplicator.TryMarkAsProcessedAsync(eventId, appKey, ttl, cancellationToken);
    }

    /// <summary>
    /// 计算重试延迟（指数退避）
    /// </summary>
    private TimeSpan CalculateRetryDelay(int attempt)
    {
        var delay = TimeSpan.FromMilliseconds(
            Math.Min(
                _initialRetryDelay.TotalMilliseconds * Math.Pow(2, attempt),
                _maxRetryDelay.TotalMilliseconds));

        _logger?.LogDebug("重试延迟: {Delay}ms (尝试 {Attempt})", delay.TotalMilliseconds, attempt + 1);
        return delay;
    }

    /// <summary>
    /// 重置失败计数
    /// </summary>
    private void ResetFailureCount()
    {
        if (_consecutiveFailures > 0)
        {
            _consecutiveFailures = 0;
            if (!_redisAvailable)
            {
                _redisAvailable = true;
                _logger?.LogInformation("Redis 连接恢复，重新启用 Redis 去重");
            }
        }
    }

    /// <summary>
    /// 生成 Redis 键
    /// </summary>
    private string GetRedisKey(string eventId, string? appKey = null)
    {
        // 如果提供了 appKey，将其包含在键中以实现多应用隔离
        // 格式: {prefix}{appKey}:{eventId} 或 {prefix}{eventId}
        if (!string.IsNullOrEmpty(appKey))
        {
            return $"{_keyPrefix}{appKey}:{eventId}";
        }
        return $"{_keyPrefix}{eventId}";
    }
}
