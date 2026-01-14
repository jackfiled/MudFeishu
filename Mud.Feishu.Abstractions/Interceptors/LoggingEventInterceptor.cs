// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Interceptors;

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
    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始处理事件: {EventType}, EventId: {EventId}, TenantKey: {TenantKey}",
            eventType, eventData.EventId, eventData.TenantKey);
        return Task.FromResult(true);
    }

    /// <summary>
    /// 事件处理后拦截
    /// </summary>
    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        if (exception == null)
        {
            _logger.LogInformation("事件处理成功: {EventType}, EventId: {EventId}, TenantKey: {TenantKey}",
                eventType, eventData.EventId, eventData.TenantKey);
        }
        else
        {
            _logger.LogError(exception, "事件处理失败: {EventType}, EventId: {EventId}, TenantKey: {TenantKey}",
                eventType, eventData.EventId, eventData.TenantKey);
        }
        return Task.CompletedTask;
    }
}
