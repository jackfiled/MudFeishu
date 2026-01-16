// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Exceptions;

/// <summary>
/// 飞书Webhook安全异常（签名验证失败、IP白名单验证失败等）
/// </summary>
public class FeishuWebhookSecurityException : FeishuWebhookException
{
    /// <summary>
    /// 客户端IP地址
    /// </summary>
    public string? ClientIp { get; }

    /// <summary>
    /// 安全事件类型
    /// </summary>
    public SecurityEventType? SecurityEventType { get; }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookSecurityException"/> 的新实例
    /// </summary>
    public FeishuWebhookSecurityException(string message, string? clientIp = null, SecurityEventType? securityEventType = null, string? requestId = null)
        : base("SecurityError", message, requestId, isRetryable: false)
    {
        ClientIp = clientIp;
        SecurityEventType = securityEventType;
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookSecurityException"/> 的新实例
    /// </summary>
    public FeishuWebhookSecurityException(string message, Exception innerException, string? clientIp = null, SecurityEventType? securityEventType = null, string? requestId = null)
        : base("SecurityError", message, innerException, requestId, isRetryable: false)
    {
        ClientIp = clientIp;
        SecurityEventType = securityEventType;
    }

    /// <summary>
    /// 初始化 <see cref="FeishuWebhookSecurityException"/> 的新实例（带错误代码）
    /// </summary>
    public FeishuWebhookSecurityException(int errorCode, string message, string? clientIp = null, SecurityEventType? securityEventType = null, string? requestId = null)
        : base(errorCode, "SecurityError", message, requestId, isRetryable: false)
    {
        ClientIp = clientIp;
        SecurityEventType = securityEventType;
    }

    /// <summary>
    /// 返回包含完整追踪信息的字符串表示
    /// </summary>
    public override string ToString()
    {
        var baseInfo = base.ToString();
        var clientIpInfo = !string.IsNullOrEmpty(ClientIp) ? $", ClientIp: {ClientIp}" : "";
        var securityTypeInfo = SecurityEventType.HasValue ? $", SecurityEventType: {SecurityEventType}" : "";
        return baseInfo.Replace("}", $"}}{clientIpInfo}{securityTypeInfo}");
    }
}
