// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Configuration;

/// <summary>
/// 飞书 Webhook 事件处理配置
/// </summary>
public class FeishuWebhookOptions
{
    /// <summary>
    /// 应用验证 Token，用于飞书事件订阅验证
    /// </summary>
    public string VerificationToken { get; set; } = string.Empty;

    /// <summary>
    /// 事件加密 Key，用于解密飞书推送的事件数据
    /// </summary>
    public string EncryptKey { get; set; } = string.Empty;

    /// <summary>
    /// 多机器人密钥配置（AppId -> EncryptKey 映射）
    /// 支持多个飞书机器人共享同一个 Webhook 端点
    /// </summary>
    public Dictionary<string, string> MultiAppEncryptKeys { get; set; } = new();

    /// <summary>
    /// 默认应用 ID（用于多机器人场景下的回退）
    /// </summary>
    public string DefaultAppId { get; set; } = string.Empty;

    /// <summary>
    /// Webhook 路由前缀
    /// </summary>
    public string RoutePrefix { get; set; } = "feishu/Webhook";

    /// <summary>
    /// 是否自动注册 Webhook 端点
    /// </summary>
    public bool AutoRegisterEndpoint { get; set; } = true;

    /// <summary>
    /// 是否启用请求日志记录
    /// </summary>
    public bool EnableRequestLogging { get; set; } = true;

    /// <summary>
    /// 是否启用事件处理异常捕获
    /// </summary>
    public bool EnableExceptionHandling { get; set; } = true;

    /// <summary>
    /// 事件处理超时时间（毫秒）
    /// 超过此时间仍未完成的请求将被取消并返回超时错误
    /// </summary>
    public int EventHandlingTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// 并行处理事件的最大并发数
    /// </summary>
    public int MaxConcurrentEvents { get; set; } = 10;

    /// <summary>
    /// 是否启用事件处理性能监控
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = false;

    /// <summary>
    /// 支持的 HTTP 方法
    /// </summary>
    public HashSet<string> AllowedHttpMethods { get; set; } = ["POST"];

    /// <summary>
    /// 最大请求体大小（字节）
    /// </summary>
    public long MaxRequestBodySize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// 是否验证请求来源 IP
    /// </summary>
    public bool ValidateSourceIP { get; set; } = false;

    /// <summary>
    /// 允许的源 IP 地址列表
    /// </summary>
    public HashSet<string> AllowedSourceIPs { get; set; } = [];

    /// <summary>
    /// 是否在服务层再次验证请求体签名
    /// 如果 Middleware 中已验证 X-Lark-Signature 请求头，可禁用此选项以避免重复验证
    /// </summary>
    public bool EnableBodySignatureValidation { get; set; } = true;

    /// <summary>
    /// 是否强制验证 X-Lark-Signature 请求头签名
    /// 当设置为 true 时，如果请求头中缺少签名将拒绝请求
    /// 生产环境建议设置为 true 以提高安全性
    /// </summary>
    /// <remarks>
    /// 安全警告：
    /// - 生产环境必须设置为 true，否则存在严重的安全漏洞
    /// - 仅在开发/测试环境且明确了解风险时设置为 false
    /// - 系统会在生产环境自动检测并拒绝禁用签名的配置
    /// </remarks>
    public bool EnforceHeaderSignatureValidation { get; set; } = true;

    /// <summary>
    /// 时间戳验证容错范围（秒）
    /// 用于验证请求时间戳是否在有效范围内，默认为 60 秒
    /// </summary>
    /// <remarks>
    /// 安全建议：
    /// - 生产环境建议设置为 60 秒或更短，以减少重放攻击时间窗口
    /// - 开发环境可以适当放宽到 300 秒
    /// </remarks>
    public int TimestampToleranceSeconds { get; set; } = 60;

    /// <summary>
    /// 请求频率限制配置
    /// </summary>
    public RateLimitOptions RateLimit { get; set; } = new();

    /// <summary>
    /// 是否启用异步后台处理模式
    /// 启用后会立即返回飞书成功响应，然后在后台处理事件
    /// 这可以避免飞书超时重试，适用于耗时较长的业务逻辑
    /// </summary>
    public bool EnableBackgroundProcessing { get; set; } = false;

    /// <summary>
    /// 健康检查失败率阈值（不健康）
    /// 当失败率超过此值时，健康检查返回 Unhealthy 状态
    /// 范围：0.0 - 1.0，默认 0.1 (10%)
    /// </summary>
    public double HealthCheckUnhealthyFailureRateThreshold { get; set; } = 0.1;

    /// <summary>
    /// 健康检查失败率阈值（降级）
    /// 当失败率超过此值时，健康检查返回 Degraded 状态
    /// 范围：0.0 - 1.0，默认 0.05 (5%)
    /// </summary>
    public double HealthCheckDegradedFailureRateThreshold { get; set; } = 0.05;

    /// <summary>
    /// 健康检查最小事件数阈值
    /// 达到此事件数后才开始计算失败率，避免早期数据不准确
    /// 默认 10 个事件
    /// </summary>
    public int HealthCheckMinEventsThreshold { get; set; } = 10;

    /// <summary>
    /// 是否启用断路器模式
    /// 启用后，当连续失败次数达到阈值时，断路器将开启并拒绝新请求
    /// 这可以防止级联故障，保护下游服务
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// 断路器配置
    /// </summary>
    public CircuitBreakerConfiguration CircuitBreaker { get; set; } = new();

    /// <summary>
    /// 失败事件重试配置
    /// </summary>
    public FailedEventRetryOptions Retry { get; set; } = new();

    /// <summary>
    /// 是否启用事件拦截器
    /// </summary>
    public bool EnableEventInterceptors { get; set; } = false;

    /// <summary>
    /// 验证配置的有效性
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrEmpty(VerificationToken) && EnforceHeaderSignatureValidation)
            throw new InvalidOperationException("生产环境必须配置 VerificationToken");

        if (string.IsNullOrEmpty(EncryptKey) && EnableBodySignatureValidation)
            throw new InvalidOperationException("启用请求体签名验证时必须配置 EncryptKey");

        if (EventHandlingTimeoutMs < 1000)
            throw new InvalidOperationException("EventHandlingTimeoutMs 必须至少为 1000 毫秒");

        if (MaxConcurrentEvents < 1)
            throw new InvalidOperationException("MaxConcurrentEvents 必须至少为 1");

        if (MaxRequestBodySize < 1024)
            throw new InvalidOperationException("MaxRequestBodySize 必须至少为 1024 字节");

        if (TimestampToleranceSeconds < 0)
            throw new InvalidOperationException("TimestampToleranceSeconds 不能为负数");

        // 验证断路器配置
        CircuitBreaker.Validate();

        // 验证重试配置
        Retry.Validate();
    }
}

/// <summary>
/// 断路器配置
/// </summary>
public class CircuitBreakerConfiguration
{
    /// <summary>
    /// 断路器断开前的连续失败次数，默认 5
    /// </summary>
    public int ExceptionsAllowedBeforeBreaking { get; set; } = 5;

    /// <summary>
    /// 断路器保持开启状态的持续时间（秒），默认 30 秒
    /// </summary>
    public int DurationOfBreakSeconds { get; set; } = 30;

    /// <summary>
    /// 断路器进入半开状态后的成功次数阈值，默认 3
    /// 达到此成功次数后，断路器重置为关闭状态
    /// </summary>
    public int SuccessThresholdToReset { get; set; } = 3;

    /// <summary>
    /// 验证配置的有效性
    /// </summary>
    public void Validate()
    {
        if (ExceptionsAllowedBeforeBreaking < 1)
            throw new InvalidOperationException("ExceptionsAllowedBeforeBreaking 必须至少为 1");

        if (DurationOfBreakSeconds < 1)
            throw new InvalidOperationException("DurationOfBreakSeconds 必须至少为 1");

        if (SuccessThresholdToReset < 1)
            throw new InvalidOperationException("SuccessThresholdToReset 必须至少为 1");
    }
}

/// <summary>
/// 失败事件重试配置
/// </summary>
public class FailedEventRetryOptions
{
    /// <summary>
    /// 是否启用失败事件重试
    /// </summary>
    public bool EnableRetry { get; set; } = false;

    /// <summary>
    /// 最大重试次数，默认 3
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 初始重试延迟（秒），默认 10
    /// </summary>
    public int InitialRetryDelaySeconds { get; set; } = 10;

    /// <summary>
    /// 重试延迟倍数（指数退避），默认 2
    /// </summary>
    public double RetryDelayMultiplier { get; set; } = 2.0;

    /// <summary>
    /// 最大重试延迟（秒），默认 300（5分钟）
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 300;

    /// <summary>
    /// 重试轮询间隔（秒），默认 30
    /// </summary>
    public int RetryPollIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 每次轮询处理的最大失败事件数，默认 10
    /// </summary>
    public int MaxRetryPerPoll { get; set; } = 10;

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    public void Validate()
    {
        if (MaxRetryCount < 0)
            throw new InvalidOperationException("MaxRetryCount 不能为负数");

        if (InitialRetryDelaySeconds < 1)
            throw new InvalidOperationException("InitialRetryDelaySeconds 必须至少为 1 秒");

        if (RetryDelayMultiplier < 1.0)
            throw new InvalidOperationException("RetryDelayMultiplier 必须大于等于 1.0");

        if (MaxRetryDelaySeconds < InitialRetryDelaySeconds)
            throw new InvalidOperationException("MaxRetryDelaySeconds 必须大于等于 InitialRetryDelaySeconds");

        if (RetryPollIntervalSeconds < 1)
            throw new InvalidOperationException("RetryPollIntervalSeconds 必须至少为 1 秒");

        if (MaxRetryPerPoll < 1)
            throw new InvalidOperationException("MaxRetryPerPoll 必须至少为 1");
    }
}
