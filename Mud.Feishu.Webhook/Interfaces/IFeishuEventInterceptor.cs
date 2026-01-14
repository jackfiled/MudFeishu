// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Models;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书事件处理拦截器接口
/// 允许在事件处理前后执行自定义逻辑
/// </summary>
public interface IFeishuEventInterceptor
{
    /// <summary>
    /// 事件处理前拦截
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="eventData">事件数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果返回 false，将中断事件处理流程</returns>
    Task<bool> BeforeHandleAsync(string eventType, Mud.Feishu.Abstractions.EventData eventData, CancellationToken cancellationToken);

    /// <summary>
    /// 事件处理后拦截
    /// </summary>
    /// <param name="eventType">事件类型</param>
    /// <param name="eventData">事件数据</param>
    /// <param name="exception">处理过程中的异常，如果为 null 表示处理成功</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task AfterHandleAsync(string eventType, Mud.Feishu.Abstractions.EventData eventData, Exception? exception, CancellationToken cancellationToken);
}

/// <summary>
/// 日志拦截器
/// 记录事件处理的详细日志
/// </summary>
public class LoggingEventInterceptor : IFeishuEventInterceptor
{
    private readonly ILogger<LoggingEventInterceptor> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public LoggingEventInterceptor(ILogger<LoggingEventInterceptor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 事件处理前拦截
    /// </summary>
    public Task<bool> BeforeHandleAsync(string eventType, Mud.Feishu.Abstractions.EventData eventData, CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始处理事件: {EventType}, EventId: {EventId}", eventType, eventData.EventId);
        return Task.FromResult(true);
    }

    /// <summary>
    /// 事件处理后拦截
    /// </summary>
    public Task AfterHandleAsync(string eventType, Mud.Feishu.Abstractions.EventData eventData, Exception? exception, CancellationToken cancellationToken)
    {
        if (exception == null)
        {
            _logger.LogInformation("事件处理成功: {EventType}, EventId: {EventId}", eventType, eventData.EventId);
        }
        else
        {
            _logger.LogError(exception, "事件处理失败: {EventType}, EventId: {EventId}", eventType, eventData.EventId);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// 指标拦截器
/// 收集事件处理的指标数据
/// </summary>
public class MetricsEventInterceptor : IFeishuEventInterceptor
{
    private readonly MetricsCollector _metrics;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, System.Diagnostics.Stopwatch> _stopwatches = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="metrics">指标收集器</param>
    public MetricsEventInterceptor(MetricsCollector metrics)
    {
        _metrics = metrics;
    }

    /// <summary>
    /// 事件处理前拦截
    /// </summary>
    public Task<bool> BeforeHandleAsync(string eventType, Mud.Feishu.Abstractions.EventData eventData, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _stopwatches.TryAdd(eventData.EventId ?? Guid.NewGuid().ToString(), stopwatch);
        return Task.FromResult(true);
    }

    /// <summary>
    /// 事件处理后拦截
    /// </summary>
    public Task AfterHandleAsync(string eventType, Mud.Feishu.Abstractions.EventData eventData, Exception? exception, CancellationToken cancellationToken)
    {
        var eventId = eventData.EventId ?? string.Empty;

        if (_stopwatches.TryRemove(eventId, out var stopwatch))
        {
            stopwatch.Stop();
            _metrics.RecordProcessingTime(stopwatch.ElapsedMilliseconds);

            if (exception == null)
            {
                _metrics.IncrementProcessedEvents();
            }
            else
            {
                _metrics.IncrementFailedEvents();
            }
        }

        return Task.CompletedTask;
    }
}
