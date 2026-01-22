// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Interceptors;

namespace Mud.Feishu.Webhook.Demo.Interceptors.MultiApp;

/// <summary>
/// App2 专用拦截器 - 处理用户和审批应用的特殊逻辑
/// </summary>
public class App2SpecificInterceptor : IFeishuEventInterceptor
{
    private readonly ILogger<App2SpecificInterceptor> _logger;

    public App2SpecificInterceptor(ILogger<App2SpecificInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[App2拦截器] 事件开始处理: EventType={EventType}, EventId={EventId}, TenantKey={TenantKey}",
            eventType, eventData.EventId, eventData.TenantKey);

        // App2特定的前置处理逻辑
        if (eventType.StartsWith("approval"))
        {
            _logger.LogDebug("[App2拦截器] 这是审批相关事件，执行App2特定逻辑");
        }

        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "[App2拦截器] 事件处理失败: EventType={EventType}, EventId={EventId}",
                eventType, eventData.EventId);
        }
        else
        {
            _logger.LogInformation("[App2拦截器] 事件处理成功: EventType={EventType}, EventId={EventId}",
                eventType, eventData.EventId);
        }

        // App2特定的后置处理逻辑
        return Task.CompletedTask;
    }
}
