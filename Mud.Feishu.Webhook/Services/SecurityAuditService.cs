// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook;

/// <summary>
/// 安全审计服务实现
/// </summary>
public class SecurityAuditService : ISecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SecurityAuditService(ILogger<SecurityAuditService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task LogSecurityFailureAsync(
        SecurityEventType eventType,
        string clientIp,
        string requestPath,
        string details,
        string? requestId = null,
        string? appKey = null)
    {
        var message = $"安全验证失败 - 类型: {eventType}, IP: {clientIp}, 路径: {requestPath}, 详情: {details}";
        if (!string.IsNullOrEmpty(requestId))
        {
            message += $", RequestId: {requestId}";
        }
        if (!string.IsNullOrEmpty(appKey))
        {
            message += $", AppKey: {appKey}";
        }

        _logger.LogWarning(message);

        // 这里可以扩展到外部安全审计系统，如SIEM等
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogSecuritySuccessAsync(
        SecurityEventType eventType,
        string clientIp,
        string requestPath,
        string details,
        string? requestId = null,
        string? appKey = null)
    {
        var message = $"安全验证成功 - 类型: {eventType}, IP: {clientIp}, 路径: {requestPath}, 详情: {details}";
        if (!string.IsNullOrEmpty(requestId))
        {
            message += $", RequestId: {requestId}";
        }
        if (!string.IsNullOrEmpty(appKey))
        {
            message += $", AppKey: {appKey}";
        }

        _logger.LogInformation(message);

        // 这里可以扩展到外部安全审计系统，如SIEM等
        return Task.CompletedTask;
    }
}