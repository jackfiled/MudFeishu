// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Exceptions;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Serialization;
using Mud.Feishu.Webhook.Utils;
using System.Diagnostics;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书 Webhook 中间件
/// </summary>
public class FeishuWebhookMiddleware
{
    /// <summary>
    /// Activity Source 用于分布式追踪
    /// </summary>
    private static class FeishuWebhookActivitySource
    {
        public static readonly ActivitySource Source = new ActivitySource("Mud.Feishu.Webhook");
    }


    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeishuWebhookMiddleware> _logger;
    private readonly FeishuWebhookOptions _options;
    private readonly bool _isDevelopment;
    private ISecurityAuditService? _securityAuditService;
    private IThreatDetectionService? _threatDetectionService;
    private IFailedEventStore? _failedEventStore;

    /// <summary>
    /// 初始化 FeishuWebhookMiddleware 实例
    /// </summary>
    /// <param name="next">请求委托</param>
    /// <param name="scopeFactory">服务作用域工厂</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="options">Webhook配置选项</param>
    public FeishuWebhookMiddleware(
            RequestDelegate next,
            IServiceScopeFactory scopeFactory,
            ILogger<FeishuWebhookMiddleware> logger,
            IOptions<FeishuWebhookOptions> options)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
        // 检测开发环境(兼容netstandard2.0)
        _isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.ToLowerInvariant() == "development";
    }

    /// <summary>
    /// 处理 HTTP 请求
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogDebug("进入 FeishuWebhookMiddleware.InvokeAsync 方法");

        // 仅在开发环境打印详细请求信息
        if (_isDevelopment)
        {
            _logger.LogDebug(HttpContextDebugHelper.PrintRequestInfo(context.Request));
        }

        var stopwatch = Stopwatch.StartNew();
        using var scrope = _scopeFactory.CreateScope();
        _securityAuditService = scrope.ServiceProvider.GetService<ISecurityAuditService>();
        _threatDetectionService = scrope.ServiceProvider.GetService<IThreatDetectionService>();
        _failedEventStore = scrope.ServiceProvider.GetService<IFailedEventStore>();

        // 使用 RequestIdHelper 获取或生成 RequestId（支持多种追踪头）
        var requestId = RequestIdHelper.GetOrGenerateRequestId(context);
        var requestIdSource = RequestIdHelper.GetRequestIdSource(context);

        _logger.LogDebug("开始处理飞书 Webhook 请求, RequestId: {RequestId}, 来源: {RequestIdSource}", requestId, requestIdSource);

        // 开始分布式追踪 Activity
        using var activity = FeishuWebhookActivitySource.Source.StartActivity("FeishuWebhook.Invoke");
        activity?.SetTag("request.id", requestId);
        activity?.SetTag("request.path", context.Request.Path);
        activity?.SetTag("request.method", context.Request.Method);

        // 记录请求信息（放在try块外，以便catch块中使用）
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        activity?.SetTag("request.client_ip", clientIp);
        RequestIdHelper.SetActivityRequestId(activity, context);

        try
        {
            // 检查请求路径是否匹配
            if (!IsTargetRequest(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // 检查 HTTP 方法
            if (!_options.AllowedHttpMethods.Contains(context.Request.Method))
            {
                throw new FeishuWebhookValidationException(
                    $"不支持的HTTP方法: {context.Request.Method}",
                    fieldName: "Method",
                    requestId: requestId);
            }

            // 检查 Content-Type
            var contentType = context.Request.ContentType;
            if (string.IsNullOrEmpty(contentType) ||
#if NET5_0_OR_GREATER
                !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
#else
                !contentType.Contains("application/json"))
#endif
            {
                throw new FeishuWebhookValidationException(
                    $"不支持的 Content-Type: {contentType}",
                    fieldName: "Content-Type",
                    requestId: requestId);
            }

            // 检查请求体大小
            if (context.Request.ContentLength > _options.MaxRequestBodySize)
            {
                throw new FeishuWebhookValidationException(
                    $"请求体大小 {context.Request.ContentLength ?? 0} 超过限制 {_options.MaxRequestBodySize}",
                    fieldName: "Content-Length",
                    requestId: requestId);
            }

            // 验证来源 IP（如果启用）
            if (_options.ValidateSourceIP && !ValidateSourceIP(context.Connection.RemoteIpAddress?.ToString()))
            {
                throw new FeishuWebhookSecurityException(
                    $"IP地址不在白名单中: {clientIp}",
                    clientIp: clientIp,
                    securityEventType: SecurityEventType.IpValidation,
                    requestId: requestId);
            }

            // 读取请求体
            var requestBody = await ReadRequestBodyAsync(context.Request);
            if (string.IsNullOrEmpty(requestBody))
            {
                throw new FeishuWebhookValidationException(
                    "请求体为空",
                    fieldName: "Body",
                    requestId: requestId);
            }

            // 调试日志：显示请求体
            _logger.LogDebug("收到请求体（前200字符）: {RequestBodyPrefix}", requestBody.Length > 200 ? requestBody.Substring(0, 200) + "..." : requestBody);

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
                            throw new FeishuWebhookSecurityException(
                                $"检测到威胁: {threatResult.Description}",
                                clientIp: clientIp,
                                securityEventType: SecurityEventType.ThreatDetection,
                                requestId: requestId);
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
        catch (FeishuWebhookSecurityException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.message", ex.Message },
                { "exception.type", ex.GetType().Name },
                { "exception.stacktrace", ex.StackTrace ?? string.Empty },
                { "exception.error_type", ex.ErrorType },
                { "exception.security_event_type", ex.SecurityEventType?.ToString() ?? "null" }
            }));
            _logger.LogError(ex, "飞书 Webhook 安全异常, RequestId: {RequestId}, ClientIp: {ClientIp}", requestId, ex.ClientIp);

            // 记录安全审计日志
            _ = _securityAuditService?.LogSecurityFailureAsync(
                ex.SecurityEventType ?? SecurityEventType.Other,
                ex.ClientIp ?? clientIp,
                context.Request.Path,
                ex.Message,
                requestId);

            await WriteErrorResponse(context, 403, $"Forbidden: {ex.Message}", requestId);
        }
        catch (FeishuWebhookValidationException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.message", ex.Message },
                { "exception.type", ex.GetType().Name },
                { "exception.stacktrace", ex.StackTrace ?? string.Empty },
                { "exception.error_type", ex.ErrorType },
                { "exception.field_name", ex.FieldName ?? "null" }
            }));
            _logger.LogWarning(ex, "飞书 Webhook 验证异常, RequestId: {RequestId}", requestId);
            await WriteErrorResponse(context, 400, $"Bad Request: {ex.Message}", requestId);
        }
        catch (FeishuWebhookException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.message", ex.Message },
                { "exception.type", ex.GetType().Name },
                { "exception.stacktrace", ex.StackTrace ?? string.Empty },
                { "exception.error_type", ex.ErrorType },
                { "exception.is_retryable", ex.IsRetryable }
            }));
            _logger.LogError(ex, "飞书 Webhook 异常, RequestId: {RequestId}, ErrorType: {ErrorType}", requestId, ex.ErrorType);
            await WriteErrorResponse(context, 500, $"Internal Server Error: {ex.Message}", requestId);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.message", ex.Message },
                { "exception.type", ex.GetType().Name },
                { "exception.stacktrace", ex.StackTrace ?? string.Empty }
            }));
            _logger.LogError(ex, "处理飞书 Webhook 请求时发生未预期的错误, RequestId: {RequestId}", requestId);

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
            activity?.SetTag("request.duration_ms", stopwatch.ElapsedMilliseconds);

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

        // 使用 IP 地址验证工具（支持 CIDR 格式）
        var isValid = IpAddressHelper.IsIpAllowed(remoteIpAddress, _options.AllowedSourceIPs);
        if (!isValid)
        {
            _logger.LogWarning("请求来源 IP {RemoteIP} 不在允许列表中，拒绝请求", remoteIpAddress);
        }

        return isValid;
    }

    /// <summary>
    /// 读取请求体（流式读取，实时验证大小）
    /// </summary>
    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        // 如果已经有 Content-Length，先检查大小
        if (request.ContentLength.HasValue && request.ContentLength.Value > _options.MaxRequestBodySize)
        {
            throw new InvalidOperationException($"请求体大小 {request.ContentLength.Value} 超过限制 {_options.MaxRequestBodySize}");
        }

        request.EnableBuffering();

        // 流式读取请求体，实时验证大小，防止 Content-Length 伪造攻击
        var buffer = new MemoryStream();
        var tempBuffer = new byte[8192]; // 8KB 缓冲区

        int bytesRead;
        while ((bytesRead = await request.Body.ReadAsync(tempBuffer, 0, tempBuffer.Length)) > 0)
        {
            buffer.Write(tempBuffer, 0, bytesRead);

            // 实时检查大小，防止伪造 Content-Length 的 DoS 攻击
            if (buffer.Length > _options.MaxRequestBodySize)
            {
                request.Body.Position = 0;
                throw new InvalidOperationException($"请求体大小 {buffer.Length} 超过限制 {_options.MaxRequestBodySize}");
            }
        }

        buffer.Position = 0;
        var body = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();
        request.Body.Position = 0;

        return body;
    }

    /// <summary>
    /// 处理 Webhook 请求
    /// </summary>
    private async Task ProcessWebhookRequest(HttpContext context, string requestBody, string requestId, string clientIp)
    {
        using var processActivity = FeishuWebhookActivitySource.Source.StartActivity("FeishuWebhook.Process");
        processActivity?.SetTag("request.id", requestId);
        processActivity?.SetTag("request.body_length", requestBody.Length);

        using var scope = _scopeFactory.CreateScope();
        var webhookService = scope.ServiceProvider.GetRequiredService<IFeishuWebhookService>();
        var validator = scope.ServiceProvider.GetRequiredService<IFeishuEventValidator>();

        try
        {
            _logger.LogInformation("开始处理 Webhook 请求, RequestId: {RequestId}", requestId);

            // 步骤 1：尝试处理明文 URL 验证请求
            if (await TryHandlePlaintextVerificationAsync(context, requestBody, webhookService))
            {
                return;
            }

            // 步骤 2：解析加密请求
            var eventRequest = JsonSerializer.Deserialize(
                requestBody,
                FeishuJsonContext.Default.FeishuWebhookRequest);

            if (eventRequest == null || string.IsNullOrEmpty(eventRequest.Encrypt))
            {
                throw new FeishuWebhookValidationException(
                    "请求格式无效：缺少 encrypt 字段",
                    fieldName: "Encrypt",
                    requestId: requestId);
            }

            // 步骤 3：解密请求数据
            var encryptKey = GetEncryptKey(eventRequest);
            var decryptor = scope.ServiceProvider.GetRequiredService<IFeishuEventDecryptor>();
            var decryptedData = await decryptor.DecryptAsync(eventRequest.Encrypt!, encryptKey);

            if (decryptedData == null)
            {
                _logger.LogError("解密失败，返回null，尝试将加密内容作为验证请求处理");
                // 解密失败可能是验证请求格式与 EventData 不匹配
                // 尝试直接解密为字符串并解析为验证请求
                await TryHandleEncryptedVerificationAsync(context, eventRequest.Encrypt, encryptKey, requestId);
                return;
            }

            _logger.LogInformation("解密成功 - EventType: [{EventType}], EventId: [{EventId}], AppId: [{AppId}], TenantKey: [{TenantKey}]",
                decryptedData.EventType ?? "(null)",
                decryptedData.EventId ?? "(null)",
                decryptedData.AppId ?? "(null)",
                decryptedData.TenantKey ?? "(null)");

            // 步骤 4：检查是否为加密验证请求（事件类型为空也可能是验证请求）
            if (decryptedData.EventType == "url_verification" ||
                (string.IsNullOrEmpty(decryptedData.EventType) && string.IsNullOrEmpty(decryptedData.EventId)))
            {
                // 如果 Event 为空或 null，说明 EventData 解析不正确
                // 尝试直接解密为验证请求格式
                if (decryptedData.Event == null)
                {
                    await TryHandleEncryptedVerificationAsync(context, eventRequest.Encrypt, encryptKey, requestId);
                    return;
                }

                await HandleEncryptedVerificationAsync(context, decryptedData);
                return;
            }

            // 步骤 5：验证签名（使用 EncryptKey）
            await ValidateRequestSignatureAsync(context, requestBody, encryptKey, clientIp, requestId, validator);

            // 步骤 6：处理事件请求
            await HandleEventRequestAsync(context, decryptedData, webhookService, requestId);
        }
        catch (JsonException ex)
        {
            processActivity?.SetStatus(ActivityStatusCode.Error, "Invalid JSON");
            processActivity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.message", ex.Message },
                { "exception.type", ex.GetType().Name },
                { "exception.stacktrace", ex.StackTrace ?? string.Empty }
            }));
            _logger.LogError(ex, "反序列化请求体时发生错误, RequestId: {RequestId}", requestId);
            throw new FeishuWebhookSerializationException(
                "JSON格式无效",
                ex,
                format: "JSON",
                requestId: requestId);
        }
    }

    /// <summary>
    /// 尝试直接解密加密验证请求（不使用 EventData 格式）
    /// </summary>
    private async Task TryHandleEncryptedVerificationAsync(HttpContext context, string encryptData, string encryptKey, string requestId)
    {
        try
        {
            // 直接解密
            var encryptedBytes = Convert.FromBase64String(encryptData);
            var decryptedJson = await DecryptAes256CbcAsync(encryptedBytes, encryptKey);

            if (string.IsNullOrEmpty(decryptedJson))
            {
                throw new FeishuWebhookDecryptionException(
                    "解密验证请求失败",
                    requestId: requestId);
            }

            // 解析为验证请求
            var verificationRequest = JsonSerializer.Deserialize<EventVerificationRequest>(
                decryptedJson,
                FeishuJsonContext.Default.EventVerificationRequest);

            if (verificationRequest?.Type == "url_verification")
            {
                _logger.LogInformation("成功解析为加密验证请求，返回挑战码");
                var verificationResponse = new EventVerificationResponse
                {
                    Challenge = verificationRequest.Challenge ?? string.Empty
                };
                await WriteJsonResponse(context, 200, verificationResponse);
                return;
            }

            throw new FeishuWebhookValidationException(
                "无效的验证请求格式",
                requestId: requestId);
        }
        catch (FeishuWebhookException)
        {
            // 重新抛出自定义异常
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理加密验证请求时发生错误");
            throw new FeishuWebhookDecryptionException(
                "处理验证请求失败",
                ex,
                requestId: requestId);
        }
    }

    /// <summary>
    /// AES-256-CBC 解密辅助方法
    /// </summary>
    private async Task<string?> DecryptAes256CbcAsync(byte[] encryptedBytes, string encryptKey)
    {
        const int blockSize = 16;
        return await Task.Run(() =>
        {
            try
            {
                // 飞书的 EncryptKey 直接使用 UTF-8 编码后进行 SHA-256 哈希得到 32 字节密钥
                byte[] keyBytes;
                using (var sha256 = SHA256.Create())
                {
                    keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptKey));
                }

                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.Mode = CipherMode.CBC;
                aes.IV = encryptedBytes.Take(blockSize).ToArray();
                aes.Padding = PaddingMode.PKCS7;
                using var transform = aes.CreateDecryptor();
                var result = transform.TransformFinalBlock(encryptedBytes, blockSize, encryptedBytes.Length - blockSize);
                return Encoding.UTF8.GetString(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AES 解密失败");
                return null;
            }
        });
    }

    /// <summary>
    /// 尝试处理明文 URL 验证请求
    /// </summary>
    private async Task<bool> TryHandlePlaintextVerificationAsync(HttpContext context, string requestBody, IFeishuWebhookService webhookService)
    {
        var verificationRequest = JsonSerializer.Deserialize(
            requestBody,
            FeishuJsonContext.Default.EventVerificationRequest);

        if (verificationRequest?.Type == "url_verification")
        {
            _logger.LogDebug("检测到明文 URL 验证请求");
            var verificationResponse = await webhookService.VerifyEventSubscriptionAsync(verificationRequest);
            if (verificationResponse != null)
            {
                _logger.LogInformation("明文验证成功，返回挑战码");
                await WriteJsonResponse(context, 200, verificationResponse);
                return true;
            }
            else
            {
                _logger.LogWarning("明文验证失败");
            }
        }

        return false;
    }

    /// <summary>
    /// 处理加密的 URL 验证请求
    /// </summary>
    private async Task HandleEncryptedVerificationAsync(HttpContext context, EventData decryptedData)
    {
        string? challenge;
        if (decryptedData.Event is JsonElement eventElement)
        {
            challenge = eventElement.ValueKind == JsonValueKind.String
                ? eventElement.GetString()
                : string.Empty;
        }
        else
        {
            challenge = decryptedData.Event?.ToString() ?? string.Empty;
        }

        var verificationResponse = new EventVerificationResponse
        {
            Challenge = challenge ?? string.Empty
        };

        _logger.LogInformation("加密验证成功，返回挑战码");
        await WriteJsonResponse(context, 200, verificationResponse);
    }

    /// <summary>
    /// 验证请求签名
    /// </summary>
    private async Task ValidateRequestSignatureAsync(HttpContext context, string requestBody, string encryptKey, string clientIp, string requestId, IFeishuEventValidator validator)
    {
        var headerSignature = context.Request.Headers["X-Lark-Signature"].FirstOrDefault();
        var headerTimestamp = context.Request.Headers["X-Lark-Request-Timestamp"].FirstOrDefault();
        var headerNonce = context.Request.Headers["X-Lark-Request-Nonce"].FirstOrDefault();

        // 解析时间戳
        long timestamp = 0;
        if (!string.IsNullOrEmpty(headerTimestamp) && long.TryParse(headerTimestamp, out var parsedTimestamp))
        {
            timestamp = parsedTimestamp;
        }

        // 使用请求头中的 Nonce 和 Timestamp，而不是请求体中的
        if (!await validator.ValidateHeaderSignatureAsync(
            timestamp,
            headerNonce ?? string.Empty,
            requestBody,
            headerSignature,
            encryptKey))
        {
            throw new FeishuWebhookSecurityException(
                "X-Lark-Signature 请求头签名验证失败",
                clientIp: clientIp,
                securityEventType: SecurityEventType.SignatureValidation,
                requestId: requestId);
        }

        _ = _securityAuditService?.LogSecuritySuccessAsync(
            SecurityEventType.SignatureValidation,
            clientIp,
            context.Request.Path,
            "X-Lark-Signature 请求头签名验证成功",
            requestId);
    }

    /// <summary>
    /// 处理事件请求
    /// </summary>
    private async Task HandleEventRequestAsync(HttpContext context, EventData eventData, IFeishuWebhookService webhookService, string requestId)
    {
        if (_options.EnableBackgroundProcessing)
        {
            // 后台处理模式：先返回成功响应，再异步处理
            await WriteJsonResponse(context, 200, new { });

            // 后台处理（使用 Task.Run 避免阻塞请求线程）
            _ = Task.Run(async () =>
            {
                using var backgroundActivity = FeishuWebhookActivitySource.Source.StartActivity("FeishuWebhook.BackgroundProcess");
                backgroundActivity?.SetTag("request.id", requestId);

                try
                {
                    var token = CancellationToken.None;
                    var result = await webhookService.HandleEventAsync(eventData, token);

                    if (result.Success)
                    {
                        backgroundActivity?.SetStatus(ActivityStatusCode.Ok);
                        _logger.LogInformation("后台事件处理完成, RequestId: {RequestId}", requestId);
                    }
                    else
                    {
                        backgroundActivity?.SetStatus(ActivityStatusCode.Error, result.ErrorReason ?? "Unknown error");
                        _logger.LogError("后台事件处理失败, RequestId: {RequestId}, 错误: {ErrorReason}",
                            requestId, result.ErrorReason);

                        // 如果启用了失败事件存储，保存失败事件
                        if (_failedEventStore != null)
                        {
                            try
                            {
                                await _failedEventStore.StoreFailedEventAsync(
                                    eventData,
                                    new FeishuWebhookProcessingException(
                                        result.ErrorReason ?? "Unknown error",
                                        eventType: eventData.EventType,
                                        eventId: eventData.EventId,
                                        requestId: requestId),
                                    CancellationToken.None);

                                _logger.LogInformation("失败事件已保存到存储, RequestId: {RequestId}", requestId);
                            }
                            catch (Exception storeEx)
                            {
                                _logger.LogError(storeEx, "保存失败事件到存储失败, RequestId: {RequestId}", requestId);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    backgroundActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    backgroundActivity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
                    {
                        { "exception.message", ex.Message },
                        { "exception.type", ex.GetType().Name },
                        { "exception.stacktrace", ex.StackTrace ?? string.Empty }
                    }));
                    _logger.LogError(ex, "后台事件处理失败, RequestId: {RequestId}", requestId);

                    // 实现失败事件持久化存储
                    if (_failedEventStore != null)
                    {
                        try
                        {
                            await _failedEventStore.StoreFailedEventAsync(
                                eventData,
                                new FeishuWebhookProcessingException(
                                    ex.Message,
                                    ex,
                                    eventType: eventData.EventType,
                                    eventId: eventData.EventId,
                                    requestId: requestId),
                                CancellationToken.None);

                            _logger.LogInformation("失败事件已保存到存储, RequestId: {RequestId}", requestId);
                        }
                        catch (Exception storeEx)
                        {
                            _logger.LogError(storeEx, "保存失败事件到存储失败, RequestId: {RequestId}", requestId);
                        }
                    }
                }
            });

            return;
        }

        // 同步处理模式
        var token = CancellationToken.None;
        var result = await webhookService.HandleEventAsync(eventData, token);
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
    }

    /// <summary>
    /// 获取安全的错误消息（不泄露内部信息）
    /// </summary>
    private string GetSafeErrorMessage(string errorReason)
    {
        // 开发环境可以返回详细错误，生产环境返回通用错误
        if (_isDevelopment)
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
    /// 获取正确的验证令牌（用于签名验证，支持多应用场景）
    /// </summary>
    private string GetVerificationToken(FeishuWebhookRequest request)
    {
        // 如果已配置了多应用且请求中有 AppId，使用对应的 VerificationToken
        if (_options.MultiAppVerificationTokens.Any() && !string.IsNullOrEmpty(request.AppId))
        {
            if (_options.MultiAppVerificationTokens.TryGetValue(request.AppId, out var token))
            {
                _logger.LogDebug("使用多应用验证令牌配置，AppId: {AppId}", request.AppId);
                return token;
            }

            // 如果找不到对应令牌，使用默认令牌
            if (!string.IsNullOrEmpty(_options.DefaultAppId) &&
                _options.MultiAppVerificationTokens.TryGetValue(_options.DefaultAppId, out var defaultToken))
            {
                _logger.LogWarning("未找到 AppId {AppId} 对应的验证令牌，使用默认令牌", request.AppId);
                return defaultToken;
            }

            // 回退到主令牌
            _logger.LogWarning("未找到 AppId {AppId} 对应的验证令牌，使用主令牌", request.AppId);
        }

        return _options.VerificationToken;
    }

    /// <summary>
    /// 写入 JSON 响应
    /// </summary>
    private async Task WriteJsonResponse<T>(HttpContext context, int statusCode, T data)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        // 添加 RequestId 到响应头
        RequestIdHelper.AddRequestIdToResponse(context);

        var json = JsonSerializer.Serialize(data, FeishuJsonOptions.Serialize);
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// 写入错误响应
    /// </summary>
    private async Task WriteErrorResponse(HttpContext context, int statusCode, string message, string? requestId = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        // 优先使用 HttpContext 中的 RequestId
        if (string.IsNullOrEmpty(requestId))
        {
            requestId = context.Items[RequestIdHelper.RequestIdItemKey] as string;
        }

        // 添加 RequestId 到响应头
        RequestIdHelper.AddRequestIdToResponse(context);

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


public static class HttpContextDebugHelper
{
    /// <summary>
    /// 打印HttpContext基础信息
    /// </summary>
    public static string PrintBasicInfo(HttpContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== HTTP CONTEXT BASIC INFO ===");
        sb.AppendLine($"Request ID: {context.TraceIdentifier}");
        sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine($"Session ID: {context.Session?.Id ?? "No Session"}");
        sb.AppendLine($"User Authenticated: {context.User?.Identity?.IsAuthenticated ?? false}");
        sb.AppendLine($"User Name: {context.User?.Identity?.Name ?? "Anonymous"}");

        return sb.ToString();
    }

    /// <summary>
    /// 打印请求信息
    /// </summary>
    public static string PrintRequestInfo(HttpRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== REQUEST INFO ===");
        sb.AppendLine($"Method: {request.Method}");
        sb.AppendLine($"Scheme: {request.Scheme}");
        sb.AppendLine($"Host: {request.Host}");
        sb.AppendLine($"Path: {request.Path}");
        sb.AppendLine($"QueryString: {request.QueryString}");
        sb.AppendLine($"ContentType: {request.ContentType}");
        sb.AppendLine($"ContentLength: {request.ContentLength?.ToString() ?? "N/A"}");
        sb.AppendLine($"Protocol: {request.Protocol}");
        sb.AppendLine($"IsHttps: {request.IsHttps}");

        // Headers
        sb.AppendLine("\n--- Request Headers ---");
        foreach (var header in request.Headers)
        {
            sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value.ToString())}");
        }

        // Query Parameters
        sb.AppendLine("\n--- Query Parameters ---");
        foreach (var query in request.Query)
        {
            sb.AppendLine($"  {query.Key}: {string.Join(", ", query.Value.ToString())}");
        }

        // Cookies
        sb.AppendLine("\n--- Request Cookies ---");
        foreach (var cookie in request.Cookies)
        {
            sb.AppendLine($"  {cookie.Key}: {cookie.Value}");
        }

        return sb.ToString();
    }

}