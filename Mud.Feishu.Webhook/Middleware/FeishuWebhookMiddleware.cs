// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Services;
using System.Diagnostics;

namespace Mud.Feishu.Webhook.Middleware;

/// <summary>
/// 飞书 Webhook 中间件
/// </summary>
public class FeishuWebhookMiddleware(
    RequestDelegate next,
    IServiceScopeFactory scopeFactory,
    ILogger<FeishuWebhookMiddleware> logger,
    IOptions<FeishuWebhookOptions> options,
    ISecurityAuditService? securityAuditService = null,
    IThreatDetectionService? threatDetectionService = null)
{
    private readonly RequestDelegate _next = next;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<FeishuWebhookMiddleware> _logger = logger;
    private readonly FeishuWebhookOptions _options = options.Value;
    private readonly ISecurityAuditService? _securityAuditService = securityAuditService;
    private readonly IThreatDetectionService? _threatDetectionService = threatDetectionService;

    /// <summary>
    /// 处理 HTTP 请求
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // 生成请求追踪 ID
        var requestId = Guid.NewGuid().ToString();
        context.Items["RequestId"] = requestId;

        try
        {
            // 检查请求路径是否匹配
            if (!IsTargetRequest(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // 记录请求信息
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // 检查 HTTP 方法
            if (!_options.AllowedHttpMethods.Contains(context.Request.Method))
            {
                // 记录安全审计日志
                _ = _securityAuditService?.LogSecurityFailureAsync(
                    SecurityEventType.Other,
                    clientIp,
                    context.Request.Path,
                    $"不支持的HTTP方法: {context.Request.Method}",
                    requestId);

                await WriteErrorResponse(context, 405, "Method Not Allowed", requestId);
                return;
            }

            // 检查请求体大小
            if (context.Request.ContentLength > _options.MaxRequestBodySize)
            {
                // 记录安全审计日志
                _ = _securityAuditService?.LogSecurityFailureAsync(
                    SecurityEventType.RequestSizeLimit,
                    clientIp,
                    context.Request.Path,
                    $"请求体大小 {context.Request.ContentLength ?? 0} 超过限制 {_options.MaxRequestBodySize}",
                    requestId);

                await WriteErrorResponse(context, 413, "Request Entity Too Large", requestId);
                return;
            }

            // 验证来源 IP（如果启用）
            if (_options.ValidateSourceIP && !ValidateSourceIP(context.Connection.RemoteIpAddress?.ToString()))
            {
                // 记录安全审计日志
                _ = _securityAuditService?.LogSecurityFailureAsync(
                    SecurityEventType.IpValidation,
                    clientIp,
                    context.Request.Path,
                    $"IP地址不在白名单中: {clientIp}",
                    requestId);

                await WriteErrorResponse(context, 403, "Forbidden", requestId);
                return;
            }

            // 读取请求体
            var requestBody = await ReadRequestBodyAsync(context.Request);
            if (string.IsNullOrEmpty(requestBody))
            {
                await WriteErrorResponse(context, 400, "Bad Request: Empty request body", requestId);
                return;
            }

            // 进行威胁检测
            if (_threatDetectionService != null)
            {
                var threatResult = await _threatDetectionService.AnalyzeRequestAsync(
                    clientIp,
                    context.Request.Path,
                    context.Request.Method,
                    context.Request.Headers,
                    requestBody,
                    requestId);

                if (threatResult.IsThreat)
                {
                    _logger.LogWarning("检测到威胁: {Description}, 等级: {Level}, 建议操作: {Action}",
                        threatResult.Description, threatResult.ThreatLevel, threatResult.RecommendedAction);

                    // 根据威胁等级和建议操作采取相应措施
                    switch (threatResult.RecommendedAction)
                    {
                        case ThreatAction.Block:
                            await WriteErrorResponse(context, 403, "Forbidden: Potential threat detected", requestId);
                            return;
                        case ThreatAction.RateLimit:
                            // 这里可以触发额外的限流措施
                            break;
                        case ThreatAction.Log:
                            // 已经记录了日志，继续处理
                            break;
                        case ThreatAction.Allow:
                        default:
                            // 允许请求继续
                            break;
                    }
                }
            }

            // 记录请求日志
            if (_options.EnableRequestLogging)
            {
                _logger.LogInformation("收到飞书 Webhook 请求，路径: {Path}, 方法: {Method}, 内容长度: {ContentLength}, RequestId: {RequestId}",
                    context.Request.Path, context.Request.Method, requestBody.Length, requestId);
            }

            // 处理请求
            await ProcessWebhookRequest(context, requestBody, requestId, clientIp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理飞书 Webhook 请求时发生错误, RequestId: {RequestId}", requestId);

            if (_options.EnableExceptionHandling)
            {
                await WriteErrorResponse(context, 500, "Internal Server Error", requestId);
            }
            else
            {
                throw;
            }
        }
        finally
        {
            stopwatch.Stop();

            if (_options.EnablePerformanceMonitoring)
            {
                _logger.LogDebug("飞书 Webhook 请求处理完成，耗时: {ElapsedMs}ms, RequestId: {RequestId}", stopwatch.ElapsedMilliseconds, requestId);
            }
        }
    }

    /// <summary>
    /// 检查是否为目标请求
    /// </summary>
    private bool IsTargetRequest(string path)
    {
        return path.StartsWith($"/{_options.RoutePrefix}", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 验证来源 IP
    /// </summary>
    private bool ValidateSourceIP(string? remoteIpAddress)
    {
        // 如果未启用 IP 验证，直接通过
        if (!_options.ValidateSourceIP)
        {
            return true;
        }

        // 如果启用 IP 验证但没有配置允许列表，拒绝请求（安全加固）
        if (_options.AllowedSourceIPs.Count == 0)
        {
            _logger.LogWarning("已启用 IP 验证但未配置允许列表，拒绝请求");
            return false;
        }

        // 如果无法获取真实 IP，拒绝请求（安全加固）
        if (string.IsNullOrEmpty(remoteIpAddress))
        {
            _logger.LogWarning("无法获取请求来源 IP，拒绝请求");
            return false;
        }

        var isValid = _options.AllowedSourceIPs.Contains(remoteIpAddress);
        if (!isValid)
        {
            _logger.LogWarning("请求来源 IP {RemoteIP} 不在允许列表中，拒绝请求", remoteIpAddress);
        }

        return isValid;
    }

    /// <summary>
    /// 读取请求体
    /// </summary>
    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();

#if NETSTANDARD2_0
        using var reader = new StreamReader(request.Body, Encoding.UTF8, true);
#else
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
#endif
        var body = await reader.ReadToEndAsync();

        request.Body.Position = 0;

        return body;
    }

    /// <summary>
    /// 处理 Webhook 请求
    /// </summary>
    private async Task ProcessWebhookRequest(HttpContext context, string requestBody, string requestId, string clientIp)
    {
        using var scope = _scopeFactory.CreateScope();
        var webhookService = scope.ServiceProvider.GetRequiredService<IFeishuWebhookService>();
        var validator = scope.ServiceProvider.GetRequiredService<IFeishuEventValidator>();

        try
        {
            // 尝试反序列化为验证请求
            var verificationRequest = JsonSerializer.Deserialize<EventVerificationRequest>(requestBody, Configuration.FeishuJsonOptions.Deserialize);

            if (verificationRequest?.Type == "url_verification")
            {
                // 处理验证请求
                var verificationResponse = await webhookService.VerifyEventSubscriptionAsync(verificationRequest);
                if (verificationResponse != null)
                {
                    await WriteJsonResponse(context, 200, verificationResponse);
                    return;
                }
            }

            // 尝试反序列化为事件请求
            var eventRequest = JsonSerializer.Deserialize<FeishuWebhookRequest>(requestBody, Configuration.FeishuJsonOptions.Deserialize);

            if (eventRequest != null)
            {
                // 获取正确的加密密钥（支持多密钥场景）
                var encryptKey = GetEncryptKey(eventRequest);

                // 验证 X-Lark-Signature 请求头签名
                var headerSignature = context.Request.Headers["X-Lark-Signature"].FirstOrDefault();
                if (!await validator.ValidateHeaderSignatureAsync(
                    eventRequest.Timestamp,
                    eventRequest.Nonce,
                    requestBody,
                    headerSignature,
                    encryptKey))
                {
                    // 记录安全审计日志
                    _ = _securityAuditService?.LogSecurityFailureAsync(
                        SecurityEventType.SignatureValidation,
                        clientIp,
                        context.Request.Path,
                        "X-Lark-Signature 请求头签名验证失败",
                        requestId);

                    await WriteErrorResponse(context, 401, "Unauthorized: Invalid X-Lark-Signature", requestId);
                    return;
                }
                else
                {
                    // 记录签名验证成功日志
                    _ = _securityAuditService?.LogSecuritySuccessAsync(
                        SecurityEventType.SignatureValidation,
                        clientIp,
                        context.Request.Path,
                        "X-Lark-Signature 请求头签名验证成功",
                        requestId);
                }

                // 处理事件请求
                if (_options.EnableBackgroundProcessing)
                {
                    // 后台处理模式：先返回成功响应，再异步处理
                    await WriteJsonResponse(context, 200, new { });

                    // 后台处理（使用 Task.Run 避免阻塞请求线程）
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var token = CancellationToken.None;
                            await webhookService.HandleEventAsync(eventRequest, encryptKey, token);
                            _logger.LogInformation("后台事件处理完成, RequestId: {RequestId}", requestId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "后台事件处理失败, RequestId: {RequestId}", requestId);
                        }
                    });

                    return;
                }

                // 同步处理模式
                var token = CancellationToken.None;
                var result = await webhookService.HandleEventAsync(eventRequest, encryptKey, token);
                if (result.Success)
                {
                    await WriteJsonResponse(context, 200, new { });
                    return;
                }

                // 根据错误原因返回不同的状态码
                if (result.ErrorReason == "Signature validation failed")
                {
                    await WriteErrorResponse(context, 401, "Unauthorized: Invalid body signature", requestId);
                    return;
                }

                // 其他错误返回 400
                await WriteErrorResponse(context, 400, GetSafeErrorMessage(result.ErrorReason ?? "Bad Request"), requestId);
                return;
            }

            await WriteErrorResponse(context, 400, "Bad Request: Invalid request format", requestId);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "反序列化请求体时发生错误, RequestId: {RequestId}", requestId);
            await WriteErrorResponse(context, 400, "Bad Request: Invalid JSON format", requestId);
        }
    }

    /// <summary>
    /// 获取安全的错误消息（不泄露内部信息）
    /// </summary>
    private string GetSafeErrorMessage(string errorReason)
    {
        // 开发环境可以返回详细错误，生产环境返回通用错误
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

        if (isDevelopment)
        {
            return $"Bad Request: {errorReason}";
        }

        // 生产环境只返回通用错误
        return "Bad Request";
    }

    /// <summary>
    /// 获取正确的加密密钥（支持多密钥场景）
    /// </summary>
    private string GetEncryptKey(FeishuWebhookRequest request)
    {
        // 如果已配置了多密钥且请求中有 AppId，使用对应密钥
        if (_options.MultiAppEncryptKeys.Any() && !string.IsNullOrEmpty(request.AppId))
        {
            if (_options.MultiAppEncryptKeys.TryGetValue(request.AppId, out var key))
            {
                _logger.LogDebug("使用多密钥配置，AppId: {AppId}", request.AppId);
                return key;
            }

            // 如果找不到对应密钥，使用默认密钥
            if (!string.IsNullOrEmpty(_options.DefaultAppId) &&
                _options.MultiAppEncryptKeys.TryGetValue(_options.DefaultAppId, out var defaultKey))
            {
                _logger.LogWarning("未找到 AppId {AppId} 对应的密钥，使用默认密钥", request.AppId);
                return defaultKey;
            }

            // 回退到主密钥
            _logger.LogWarning("未找到 AppId {AppId} 对应的密钥，使用主密钥", request.AppId);
        }

        return _options.EncryptKey;
    }

    /// <summary>
    /// 写入 JSON 响应
    /// </summary>
    private async Task WriteJsonResponse<T>(HttpContext context, int statusCode, T data)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(data, Configuration.FeishuJsonOptions.Serialize);
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// 写入错误响应
    /// </summary>
    private async Task WriteErrorResponse(HttpContext context, int statusCode, string message, string? requestId = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            success = false,
            request_id = requestId,
            error = new
            {
                code = statusCode,
                message
            }
        };

        var json = JsonSerializer.Serialize(errorResponse, Configuration.FeishuJsonOptions.Serialize);
        await context.Response.WriteAsync(json);
    }
}
