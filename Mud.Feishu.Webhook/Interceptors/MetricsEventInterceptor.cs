// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Interceptors;
using Mud.Feishu.Webhook.Models;

namespace Mud.Feishu.Webhook;

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
    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _stopwatches.TryAdd(eventData.EventId ?? Guid.NewGuid().ToString(), stopwatch);
        return Task.FromResult(true);
    }

    /// <summary>
    /// 事件处理后拦截
    /// </summary>
    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
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
