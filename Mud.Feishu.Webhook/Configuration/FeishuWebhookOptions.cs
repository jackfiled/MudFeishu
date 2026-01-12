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
    /// 验证配置项的有效性
    /// </summary>
    /// <exception cref="InvalidOperationException">当配置项无效时抛出</exception>
    public void Validate()
    {
        // 获取当前环境
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var isProduction = string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(RoutePrefix))
            throw new InvalidOperationException("RoutePrefix不能为空");

        if (EventHandlingTimeoutMs < 1000)
            throw new InvalidOperationException("EventHandlingTimeoutMs必须至少为1000毫秒");

        if (MaxConcurrentEvents < 1)
            throw new InvalidOperationException("MaxConcurrentEvents必须至少为1");

        if (MaxRequestBodySize < 1024)
            throw new InvalidOperationException("MaxRequestBodySize必须至少为1024字节");

        if (AllowedHttpMethods == null || AllowedHttpMethods.Count == 0)
            throw new InvalidOperationException("AllowedHttpMethods不能为空");

        if (TimestampToleranceSeconds < 60)
            throw new InvalidOperationException("TimestampToleranceSeconds必须至少为60秒");

        // 验证健康检查配置
        if (HealthCheckUnhealthyFailureRateThreshold < 0 || HealthCheckUnhealthyFailureRateThreshold > 1)
            throw new InvalidOperationException("HealthCheckUnhealthyFailureRateThreshold必须在0.0-1.0之间");

        if (HealthCheckDegradedFailureRateThreshold < 0 || HealthCheckDegradedFailureRateThreshold > 1)
            throw new InvalidOperationException("HealthCheckDegradedFailureRateThreshold必须在0.0-1.0之间");

        if (HealthCheckDegradedFailureRateThreshold >= HealthCheckUnhealthyFailureRateThreshold)
            throw new InvalidOperationException("HealthCheckDegradedFailureRateThreshold必须小于HealthCheckUnhealthyFailureRateThreshold");

        if (HealthCheckMinEventsThreshold < 1)
            throw new InvalidOperationException("HealthCheckMinEventsThreshold必须至少为1");

        // 生产环境安全检查：强制启用签名验证
        if (isProduction && !EnforceHeaderSignatureValidation)
        {
            throw new InvalidOperationException(
                "生产环境必须启用 X-Lark-Signature 签名验证 (EnforceHeaderSignatureValidation = true)，" +
                "禁用签名验证存在严重的安全风险！请检查配置或设置正确的 ASPNETCORE_ENVIRONMENT 环境变量。");
        }

        // 生产环境时间戳容错警告
        if (isProduction && TimestampToleranceSeconds > 120)
        {
            throw new InvalidOperationException(
                $"生产环境的时间戳容错范围过大 ({TimestampToleranceSeconds} 秒)，" +
                "建议设置为 60 秒或更短以减少重放攻击风险。");
        }

        // 验证 EncryptKey 强度（AES-256 需要 32 字节）
        if (!string.IsNullOrEmpty(EncryptKey))
        {
            var keyBytes = Encoding.UTF8.GetByteCount(EncryptKey);
            if (keyBytes != 32)
            {
                throw new InvalidOperationException("EncryptKey 必须是 32 字节长度的字符串（用于 AES-256）");
            }

            // 检测明显不安全的默认值或弱密钥
            var weakKeys = new[]
            {
                "your_encrypt_key_here",
                "your_encrypt_key",
                "encrypt_key",
                "verification_token_here",
                "your_verification_token",
                "12345678901234567890123456789012",
                "00000000000000000000000000000000",
                "11111111111111111111111111111111",
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
            };

            if (weakKeys.Contains(EncryptKey))
            {
                throw new InvalidOperationException("EncryptKey 使用了明显不安全的默认值，请使用强密钥");
            }

            // 增强密钥强度检查
            var hasLetter = EncryptKey.Any(char.IsLetter);
            var hasDigit = EncryptKey.Any(char.IsDigit);
            var hasSpecial = EncryptKey.Any(ch => !char.IsLetterOrDigit(ch));
            var hasUpper = EncryptKey.Any(char.IsUpper);
            var hasLower = EncryptKey.Any(char.IsLower);

            // 必须同时包含字母和数字
            if (!(hasLetter && hasDigit))
            {
                throw new InvalidOperationException("EncryptKey 过于简单，应同时包含字母和数字");
            }

            // 生产环境要求包含大小写字母
            if (isProduction && !(hasUpper && hasLower))
            {
                throw new InvalidOperationException("生产环境下 EncryptKey 必须包含大小写字母以增强安全性");
            }

            // 生产环境要求包含特殊字符
            if (isProduction && !hasSpecial)
            {
                throw new InvalidOperationException("生产环境下 EncryptKey 必须包含特殊字符以增强安全性");
            }

            // 非生产环境给出警告
            if (!isProduction && !(hasUpper && hasLower))
            {
                // 记录警告（如果可用）
                // _logger?.LogWarning("建议 EncryptKey 包含大小写字母以增强安全性");
            }

            if (!isProduction && !hasSpecial)
            {
                // _logger?.LogWarning("建议 EncryptKey 包含特殊字符以增强安全性");
            }
        }

        // 验证请求频率限制配置
        RateLimit?.Validate();

        // 验证多密钥配置
        if (MultiAppEncryptKeys.Any())
        {
            foreach (var kvp in MultiAppEncryptKeys)
            {
                var appId = kvp.Key;
                var key = kvp.Value;

                if (string.IsNullOrEmpty(key))
                {
                    throw new InvalidOperationException($"多密钥配置中应用 {appId} 的 EncryptKey 不能为空");
                }

                var keyBytes = Encoding.UTF8.GetByteCount(key);
                if (keyBytes != 32)
                {
                    throw new InvalidOperationException($"多密钥配置中应用 {appId} 的 EncryptKey 必须是 32 字节长度");
                }

                // 检测弱密钥
                var weakKeys = new[]
                {
                    "your_encrypt_key_here",
                    "your_encrypt_key",
                    "encrypt_key",
                    "12345678901234567890123456789012",
                    "00000000000000000000000000000000"
                };

                if (weakKeys.Contains(key))
                {
                    throw new InvalidOperationException($"多密钥配置中应用 {appId} 的 EncryptKey 使用了不安全的默认值");
                }
            }
        }
    }
}

/// <summary>
/// 飞书事件订阅验证请求
/// </summary>
public class EventVerificationRequest
{
    /// <summary>
    /// 事件类型：url_verification
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 挑战码
    /// </summary>
    [JsonPropertyName("challenge")]
    public string Challenge { get; set; } = string.Empty;

    /// <summary>
    /// 验证 Token
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// 飞书事件订阅验证响应
/// </summary>
public class EventVerificationResponse
{
    /// <summary>
    /// 挑战码，原样返回
    /// </summary>
    [JsonPropertyName("challenge")]
    public string Challenge { get; set; } = string.Empty;
}

/// <summary>
/// 飞书 Webhook 请求模型
/// </summary>
public class FeishuWebhookRequest
{
    /// <summary>
    /// 加密的事件数据
    /// </summary>
    [JsonPropertyName("encrypt")]
    public string? Encrypt { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    /// <summary>
    /// 随机数
    /// </summary>
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    /// 签名
    /// </summary>
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// 是否为验证请求
    /// </summary>
    [JsonIgnore]
    public bool IsVerificationRequest => Type == "url_verification";

    /// <summary>
    /// 请求类型
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 挑战码（验证请求时使用）
    /// </summary>
    [JsonPropertyName("challenge")]
    public string? Challenge { get; set; }

    /// <summary>
    /// 应用 ID（解密后从事件中获取，用于多密钥场景）
    /// </summary>
    [JsonIgnore]
    public string AppId { get; set; } = string.Empty;
}