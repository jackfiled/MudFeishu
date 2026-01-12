// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 威胁检测服务接口
/// </summary>
public interface IThreatDetectionService
{
    /// <summary>
    /// 检测请求是否存在威胁
    /// </summary>
    /// <param name="clientIp">客户端IP</param>
    /// <param name="requestPath">请求路径</param>
    /// <param name="requestMethod">请求方法</param>
    /// <param name="requestHeaders">请求头</param>
    /// <param name="requestBody">请求体</param>
    /// <param name="requestId">请求ID</param>
    /// <returns>威胁检测结果</returns>
    Task<ThreatDetectionResult> AnalyzeRequestAsync(
        string clientIp,
        string requestPath,
        string requestMethod,
        IHeaderDictionary requestHeaders,
        string requestBody,
        string? requestId = null);

    /// <summary>
    /// 记录请求处理结果，用于学习正常行为模式
    /// </summary>
    /// <param name="clientIp">客户端IP</param>
    /// <param name="requestPath">请求路径</param>
    /// <param name="success">请求是否成功</param>
    /// <param name="responseTimeMs">响应时间（毫秒）</param>
    Task RecordRequestOutcomeAsync(
        string clientIp,
        string requestPath,
        bool success,
        long responseTimeMs);
}

/// <summary>
/// 威胁检测结果
/// </summary>
public class ThreatDetectionResult
{
    /// <summary>
    /// 是否检测到威胁
    /// </summary>
    public bool IsThreat { get; set; }

    /// <summary>
    /// 威胁等级（1-5，5为最高）
    /// </summary>
    public int ThreatLevel { get; set; }

    /// <summary>
    /// 威胁类型
    /// </summary>
    public ThreatType ThreatType { get; set; }

    /// <summary>
    /// 检测到的威胁描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 建议的行动
    /// </summary>
    public ThreatAction RecommendedAction { get; set; }
}

/// <summary>
/// 威胁类型
/// </summary>
public enum ThreatType
{
    /// <summary>
    /// 无威胁
    /// </summary>
    None,

    /// <summary>
    /// 异常访问模式
    /// </summary>
    AnomalousAccessPattern,

    /// <summary>
    /// 频率异常
    /// </summary>
    FrequencyAnomaly,

    /// <summary>
    /// 恶意内容
    /// </summary>
    MaliciousContent,

    /// <summary>
    /// 未知威胁
    /// </summary>
    Unknown
}

/// <summary>
/// 威胁响应动作
/// </summary>
public enum ThreatAction
{
    /// <summary>
    /// 允许请求
    /// </summary>
    Allow,

    /// <summary>
    /// 记录日志
    /// </summary>
    Log,

    /// <summary>
    /// 限流
    /// </summary>
    RateLimit,

    /// <summary>
    /// 阻止请求
    /// </summary>
    Block
}