// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书事件验证服务实现
/// </summary>
public class FeishuEventValidator : IFeishuEventValidator
{
    private readonly ILogger<FeishuEventValidator> _logger;
    private readonly IFeishuNonceDistributedDeduplicator _nonceDeduplicator;
    private readonly IOptions<FeishuWebhookOptions> _options;
    private readonly ISecurityAuditService? _securityAuditService;

    /// <summary>
    /// 当前应用键（多应用场景，使用 AsyncLocal 确保线程安全）
    /// </summary>
    private static readonly AsyncLocal<string?> _currentAppKey = new();

    /// <inheritdoc />
    public FeishuEventValidator(
        ILogger<FeishuEventValidator> logger,
        IFeishuNonceDistributedDeduplicator nonceDeduplicator,
        IOptions<FeishuWebhookOptions> options,
        ISecurityAuditService? securityAuditService)
    {
        _logger = logger;
        _nonceDeduplicator = nonceDeduplicator;
        _options = options;
        _securityAuditService = securityAuditService;
    }

    /// <summary>
    /// 设置当前应用键（多应用场景）
    /// </summary>
    public void SetCurrentAppKey(string appKey)
    {
        _currentAppKey.Value = appKey;
    }

    /// <inheritdoc />
    public bool ValidateSubscriptionRequest(EventVerificationRequest request, string expectedToken)
    {
        try
        {
            if (request.Type != "url_verification")
            {
                _logger.LogWarning("无效的验证请求类型: {Type}, AppKey: {AppKey}", request.Type, _currentAppKey.Value ?? "null");
                return false;
            }

            if (string.IsNullOrEmpty(request.Token))
            {
                _logger.LogWarning("验证请求缺少 Token, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
                return false;
            }

            if (string.IsNullOrEmpty(request.Challenge))
            {
                _logger.LogWarning("验证请求缺少 Challenge, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
                return false;
            }

            if (request.Token != expectedToken)
            {
                _logger.LogWarning("验证 Token 不匹配: 期望 {ExpectedToken}, 实际 {ActualToken}, AppKey: {AppKey}", expectedToken, request.Token, _currentAppKey.Value ?? "null");
                return false;
            }

            _logger.LogInformation("事件订阅验证请求验证成功, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证事件订阅请求时发生错误, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateSignatureAsync(long timestamp, string nonce, string encrypt, string signature, string encryptKey)
    {
        try
        {
            // 如果时间戳或 nonce 为空，跳过签名验证（飞书某些请求类型可能不包含这些字段）
            if (timestamp == 0 || string.IsNullOrEmpty(nonce))
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                var isProduction = string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);

                if (isProduction)
                {
                    _logger.LogError(
                        "时间戳或 nonce 为空（Timestamp: {Timestamp}, Nonce: {Nonce}），拒绝请求（生产环境不允许跳过签名验证）",
                        timestamp, nonce);

                    // 记录安全审计日志
                    _ = _securityAuditService?.LogSecurityFailureAsync(
                        SecurityEventType.SignatureValidation,
                        "unknown",
                        "FeishuEventValidator",
                        $"时间戳或 nonce 为空（Timestamp: {timestamp}, Nonce: {nonce}），拒绝请求",
                        "");

                    return false; // 生产环境拒绝请求
                }

                _logger.LogWarning(
                    "时间戳或 nonce 为空（Timestamp: {Timestamp}, Nonce: {Nonce}），跳过签名验证（开发环境，警告：此配置存在安全风险）",
                    timestamp, nonce);

                // 记录安全审计日志
                _ = _securityAuditService?.LogSecurityFailureAsync(
                    SecurityEventType.SignatureValidation,
                    "unknown",
                    "FeishuEventValidator",
                    $"开发环境：时间戳或 nonce 为空（Timestamp: {timestamp}, Nonce: {nonce}），跳过签名验证",
                    "");
            }

            // 检查 nonce 是否已使用（防止重放攻击）
            // TryMarkAsUsedAsync 返回 true 表示 Nonce 已被使用（重放攻击）
            // 返回 false 表示 Nonce 未被使用，并成功标记为已使用
            if (await _nonceDeduplicator.TryMarkAsUsedAsync(nonce, _currentAppKey.Value))
            {
                _logger.LogWarning("Nonce {Nonce} 已使用过（AppKey: {AppKey}），拒绝重放攻击", nonce, _currentAppKey.Value ?? "null");
                return false;
            }

            // 验证时间戳
            if (!ValidateTimestamp(timestamp, _options.Value.TimestampToleranceSeconds))
            {
                _logger.LogWarning("请求时间戳无效: {Timestamp}", timestamp);
                return false;
            }

            // 构建签名字符串
            var signString = $"{timestamp}\n{nonce}\n{encrypt}";

            // 使用 HMAC-SHA256 计算签名
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(encryptKey));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signString));
            var computedSignature = Convert.ToBase64String(hashBytes);

            // 使用固定时间比较防止计时攻击
            var computedBytes = Encoding.UTF8.GetBytes(computedSignature);
            var expectedBytes = Encoding.UTF8.GetBytes(signature);
            var isValid = FixedTimeEquals(computedBytes, expectedBytes);

            if (!isValid)
            {
                var computedPrefix = computedSignature.Length > 8 ? computedSignature.Substring(0, 8) : computedSignature;
                var signaturePrefix = signature.Length > 8 ? signature.Substring(0, 8) : signature;
                _logger.LogWarning("签名验证失败: 计算 {ComputedSignaturePrefix}..., 期望 {ExpectedSignaturePrefix}..., AppKey: {AppKey}",
                    computedPrefix + "...",
                    signaturePrefix + "...",
                    _currentAppKey.Value ?? "null");

                // 记录安全审计日志
                _ = _securityAuditService?.LogSecurityFailureAsync(
                    SecurityEventType.SignatureValidation,
                    "unknown",
                    "FeishuEventValidator",
                    $"签名验证失败: 计算 {computedPrefix}..., 期望 {signaturePrefix}...",
                    "");
            }
            else
            {
                _logger.LogDebug("签名验证成功, AppKey: {AppKey}", _currentAppKey.Value ?? "null");

                // 记录安全审计日志
                _ = _securityAuditService?.LogSecuritySuccessAsync(
                    SecurityEventType.SignatureValidation,
                    "unknown",
                    "FeishuEventValidator",
                    $"签名验证成功: {computedSignature}, AppKey: {_currentAppKey.Value ?? "null"}",
                    "");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证签名时发生错误, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateHeaderSignatureAsync(long timestamp, string nonce, string body, string? headerSignature, string encryptKey)
    {
        try
        {
            // 检查请求头签名是否为空
            if (string.IsNullOrEmpty(headerSignature))
            {
                // 如果配置为强制验证，则拒绝请求
                if (_options.Value.EnforceHeaderSignatureValidation)
                {
                    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                    var isProduction = string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);

                    _logger.LogWarning(
                        "请求头中缺少 X-Lark-Signature，拒绝请求（配置为强制验证，当前环境: {Environment}）",
                        environment);

                    // 记录安全审计日志
                    _ = _securityAuditService?.LogSecurityFailureAsync(
                        SecurityEventType.SignatureValidation,
                        "unknown",
                        "FeishuEventValidator",
                        isProduction
                            ? "生产环境：请求头缺少 X-Lark-Signature，拒绝请求"
                            : "非生产环境：请求头缺少 X-Lark-Signature（警告：此配置存在安全风险）",
                        "");

                    return false;
                }

                // 否则跳过验证（兼容旧版本）
                _logger.LogDebug(
                    "请求头中未包含 X-Lark-Signature，跳过头部签名验证（警告：此配置存在严重安全风险，" +
                    "建议在生产环境设置 EnforceHeaderSignatureValidation = true）");
                return true;
            }

            // 如果时间戳或 nonce 为空，跳过签名验证（飞书某些请求类型可能不包含这些字段）
            if (timestamp == 0 || string.IsNullOrEmpty(nonce))
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                var isProduction = string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);

                if (isProduction)
                {
                    _logger.LogError(
                        "时间戳或 nonce 为空（Timestamp: {Timestamp}, Nonce: {Nonce}），拒绝请求（生产环境不允许跳过签名验证）",
                        timestamp, nonce);

                    // 记录安全审计日志
                    _ = _securityAuditService?.LogSecurityFailureAsync(
                        SecurityEventType.SignatureValidation,
                        "unknown",
                        "FeishuEventValidator",
                        $"时间戳或 nonce 为空（Timestamp: {timestamp}, Nonce: {nonce}），拒绝请求",
                        "");

                    return false; // 生产环境拒绝请求
                }

                _logger.LogWarning(
                    "时间戳或 nonce 为空（Timestamp: {Timestamp}, Nonce: {Nonce}），跳过签名验证（开发环境，警告：此配置存在安全风险）",
                    timestamp, nonce);

                // 记录安全审计日志
                _ = _securityAuditService?.LogSecurityFailureAsync(
                    SecurityEventType.SignatureValidation,
                    "unknown",
                    "FeishuEventValidator",
                    $"开发环境：时间戳或 nonce 为空（Timestamp: {timestamp}, Nonce: {nonce}），跳过签名验证",
                    "");
            }

            // 检查 nonce 是否已使用（防止重放攻击）
            if (await _nonceDeduplicator.TryMarkAsUsedAsync(nonce, _currentAppKey.Value))
            {
                _logger.LogWarning("Nonce {Nonce} 已使用过（AppKey: {AppKey}），拒绝重放攻击", nonce, _currentAppKey.Value ?? "null");
                return false;
            }

            // 验证时间戳
            if (!ValidateTimestamp(timestamp, _options.Value.TimestampToleranceSeconds))
            {
                _logger.LogWarning("请求时间戳无效: {Timestamp}", timestamp);
                return false;
            }

            // 根据飞书官方文档，签名字符串格式为：
            // timestamp + nonce + encryptKey + body
            // 注意：这里不使用换行符连接！
            var signString = $"{timestamp}{nonce}{encryptKey}{body}";

            // 调试日志：显示签名计算信息（不记录敏感的 EncryptKey 内容，仅记录长度）
            _logger.LogDebug("请求头签名计算 - Timestamp: {Timestamp}, Nonce: {Nonce}, EncryptKey长度: {KeyLength}, Body长度: {BodyLength}",
                timestamp, nonce, encryptKey.Length, body.Length);

            // 使用 SHA-256 计算签名（不是 HMAC-SHA256！）
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(signString));
            var computedSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            // 使用固定时间比较防止计时攻击
            var isValid = !string.IsNullOrEmpty(headerSignature) &&
                FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computedSignature),
                    Encoding.UTF8.GetBytes(headerSignature));

            if (!isValid)
            {
                var computedPrefix = computedSignature.Length > 8 ? computedSignature.Substring(0, 8) : computedSignature;
                var headerPrefix = headerSignature is null ? "null" :
                    (headerSignature.Length > 8 ? headerSignature.Substring(0, 8) : headerSignature);
                _logger.LogWarning("请求头签名验证失败: 计算 {ComputedSignaturePrefix}..., 期望 {ExpectedSignaturePrefix}..., AppKey: {AppKey}",
                computedPrefix + "...",
                headerPrefix + "...",
                _currentAppKey.Value ?? "null");

                // 记录安全审计日志
                _ = _securityAuditService?.LogSecurityFailureAsync(
                    SecurityEventType.SignatureValidation,
                    "unknown",
                    "FeishuEventValidator",
                    $"请求头签名验证失败: 计算 {computedPrefix}..., 期望 {headerPrefix}...",
                    "");
            }
            else
            {
                _logger.LogDebug("请求头签名验证成功, AppKey: {AppKey}", _currentAppKey.Value ?? "null");

                // 记录安全审计日志
                _ = _securityAuditService?.LogSecuritySuccessAsync(
                    SecurityEventType.SignatureValidation,
                    "unknown",
                    "FeishuEventValidator",
                    $"请求头签名验证成功: {computedSignature}, AppKey: {_currentAppKey.Value ?? "null"}",
                    "");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证请求头签名时发生错误, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
            return false;
        }
    }

    /// <inheritdoc />
    public bool ValidateTimestamp(long timestamp, int toleranceSeconds)
    {
        try
        {
            // 如果时间戳为 0，跳过验证（飞书某些请求类型可能不包含时间戳）
            if (timestamp == 0)
            {
                _logger.LogDebug("时间戳为 0，跳过时间戳验证");
                return true;
            }

            // 判断时间戳是秒级还是毫秒级
            // 飞书 X-Lark-Request-Timestamp 请求头使用秒级时间戳（10位）
            // 飞书事件数据中的 create_time 使用毫秒级时间戳（13位）
            DateTimeOffset requestTime;
            if (timestamp < 10000000000) // 小于 100 亿，认为是秒级时间戳
            {
                requestTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
            }
            else // 大于等于 100 亿，认为是毫秒级时间戳
            {
                requestTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            }

            var now = DateTimeOffset.UtcNow;
            var diff = Math.Abs((now - requestTime).TotalSeconds);

            var isValid = diff <= toleranceSeconds;

            if (!isValid)
            {
                _logger.LogWarning("时间戳超出容错范围: 请求时间 {RequestTime}, 当前时间 {CurrentTime}, 差异 {Diff}秒, AppKey: {AppKey}",
                    requestTime, now, diff, _currentAppKey.Value ?? "null");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证时间戳时发生错误, AppKey: {AppKey}", _currentAppKey.Value ?? "null");
            return false;
        }
    }

    /// <summary>
    /// 固定时间比较方法，防止计时攻击
    /// </summary>
    /// <param name="left">第一个字节数组</param>
    /// <param name="right">第二个字节数组</param>
    /// <returns>如果两个数组相等返回 true，否则返回 false</returns>
    private static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var result = 0;
        for (var i = 0; i < left.Length; i++)
        {
            result |= left[i] ^ right[i];
        }

        return result == 0;
    }
}