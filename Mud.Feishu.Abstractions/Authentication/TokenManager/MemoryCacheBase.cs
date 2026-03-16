// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Mud.Feishu.TokenManager;

/// <summary>
/// 内存缓存基类
/// </summary>
/// <typeparam name="T">缓存值类型</typeparam>
/// <remarks>
/// 提供线程安全的内存缓存基础设施，使用 ConcurrentDictionary 实现。
/// 派生类只需实现过期判断逻辑即可。
/// </remarks>
public abstract class MemoryCacheBase<T> where T : class
{
    /// <summary>
    /// 缓存字典
    /// </summary>
    protected readonly ConcurrentDictionary<string, T> _cache = new();

    /// <summary>
    /// 日志记录器
    /// </summary>
    protected readonly ILogger _logger;

    /// <summary>
    /// 令牌刷新阈值时间（秒）
    /// </summary>
    protected readonly int _refreshThresholdSeconds;

    /// <summary>
    /// 初始化 MemoryCacheBase 实例
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="refreshThresholdSeconds">令牌刷新阈值(秒)，默认300秒</param>
    protected MemoryCacheBase(ILogger logger, int refreshThresholdSeconds = 300)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _refreshThresholdSeconds = refreshThresholdSeconds > 0 ? refreshThresholdSeconds : 300;
    }

    /// <summary>
    /// 判断缓存项是否有效
    /// </summary>
    /// <param name="entry">缓存项</param>
    /// <returns>如果有效返回 true</returns>
    protected abstract bool IsValid(T entry);

    /// <summary>
    /// 当缓存项被移除时调用
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="entry">被移除的缓存项</param>
    protected virtual void OnEntryRemoved(string key, T entry) { }

    /// <summary>
    /// 当缓存项被添加时调用
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="entry">被添加的缓存项</param>
    protected virtual void OnEntryAdded(string key, T entry) { }

    /// <summary>
    /// 从缓存中获取项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>缓存项，如果不存在或已过期则返回 null</returns>
    protected T? GetCore(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        if (_cache.TryGetValue(key, out var entry) && IsValid(entry))
        {
            LogCacheHit(key);
            return entry;
        }

        LogCacheMiss(key);
        return null;
    }

    /// <summary>
    /// 将项存入缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="entry">缓存项</param>
    protected void SetCore(string key, T entry)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        _cache.AddOrUpdate(key, entry, (k, existing) => entry);
        OnEntryAdded(key, entry);
    }

    /// <summary>
    /// 从缓存中移除项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>是否成功移除</returns>
    protected bool RemoveCore(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var removed = _cache.TryRemove(key, out var entry);
        if (removed && entry != null)
        {
            OnEntryRemoved(key, entry);
        }
        return removed;
    }

    /// <summary>
    /// 检查缓存中是否存在有效的项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>如果存在有效项返回 true</returns>
    protected bool ExistsCore(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return _cache.TryGetValue(key, out var entry) && IsValid(entry);
    }

    /// <summary>
    /// 清理过期的缓存项
    /// </summary>
    /// <returns>清理的项数</returns>
    protected int CleanExpiredCore()
    {
        var expiredKeys = _cache
            .Where(kvp => !IsValid(kvp.Value))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        return expiredKeys.Count;
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>包含总数和过期数的元组</returns>
    protected (int Total, int Expired) GetStatisticsCore()
    {
        var total = _cache.Count;
        var expired = _cache.Count(kvp => !IsValid(kvp.Value));
        return (total, expired);
    }

    /// <summary>
    /// 记录缓存命中日志
    /// </summary>
    /// <param name="key">缓存键</param>
    protected abstract void LogCacheHit(string key);

    /// <summary>
    /// 记录缓存未命中日志
    /// </summary>
    /// <param name="key">缓存键</param>
    protected abstract void LogCacheMiss(string key);
}
