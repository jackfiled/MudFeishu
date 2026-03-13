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
public sealed class FeishuEventDistributedDeduplicator : MemoryDeduplicator<string>, IFeishuEventDistributedDeduplicator
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public FeishuEventDistributedDeduplicator(
        ILogger<FeishuEventDistributedDeduplicator>? logger = null,
        TimeSpan? cacheExpiration = null,
        TimeSpan? cleanupInterval = null)
        : base(logger, cacheExpiration, cleanupInterval)
    {
        logger?.LogInformation("飞书分布式事件去重服务初始化完成，缓存过期时间: {Expiration}, 清理间隔: {CleanupInterval}",
            cacheExpiration ?? TimeSpan.FromHours(24),
            cleanupInterval ?? TimeSpan.FromMinutes(5));
    }

    /// <inheritdoc />
    public Task<bool> TryMarkAsProcessedAsync(string eventId, string? appKey = null, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            Logger?.LogWarning("事件ID为空，跳过去重检查");
            return Task.FromResult(false);
        }

        var result = TryMarkAsProcessed(eventId, appKey);

        if (result)
        {
            Logger?.LogDebug("事件 {EventId} (AppKey: {AppKey}) 已处理过，跳过", eventId, appKey ?? "default");
        }
        else
        {
            Logger?.LogDebug("事件 {EventId} (AppKey: {AppKey}) 标记为已处理", eventId, appKey ?? "default");
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<bool> IsProcessedAsync(string eventId, string? appKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(IsProcessed(eventId, appKey));
    }

    /// <inheritdoc />
    public Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CleanupExpired());
    }
}
