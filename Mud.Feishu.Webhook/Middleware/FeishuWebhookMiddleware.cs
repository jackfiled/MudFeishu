// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
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
    private ISecurityAuditService? _securityAuditService;
    private IThreatDetectionService? _threatDetectionService;
    private IFailedEventStore? _failedEventStore;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="next"></param>
    /// <param name="scopeFactory"></param>
    /// <param name="logger"></param>
    /// <param name="options"></param>
    public FeishuWebhookMiddleware(RequestDelegate next,
            IServiceScopeFactory scopeFactory,
            ILogger<FeishuWebhookMiddleware> logger,
            IOptions<FeishuWebhookOptions> options)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// 处理 HTTP 请求
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogDebug("进入 FeishuWebhookMiddleware.InvokeAsync 方法");
        var stopwatch = Stopwatch.StartNew();
        using var scrope = _scopeFactory.CreateScope();
        _securityAuditService = scrope.ServiceProvider.GetService<ISecurityAuditService>();
        _threatDetectionService = scrope.ServiceProvider.GetService<IThreatDetectionService>();
        _failedEventStore = scrope.ServiceProvider.GetService<IFailedEventStore>();
        // 生成请求追踪 ID
        var requestId = Guid.NewGuid().ToString();
        context.Items["RequestId"] = requestId;

        _logger.LogDebug("开始处理飞书 Webhook 请求, RequestId: {RequestId}", requestId);

        // 开始分布式追踪 Activity
        using var activity = FeishuWebhookActivitySource.Source.StartActivity("FeishuWebhook.Invoke");
        activity?.SetTag("request.id", requestId);
        activity?.SetTag("request.path", context.Request.Path);
        activity?.SetTag("request.method", context.Request.Method);

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
            activity?.SetTag("request.client_ip", clientIp);

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

            // 检查 Content-Type
            var contentType = context.Request.ContentType;
            if (string.IsNullOrEmpty(contentType) ||
#if NET5_0_OR_GREATER
                !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
#else
                !contentType.Contains("application/json"))
#endif
            {
                // 记录安全审计日志
                _ = _securityAuditService?.LogSecurityFailureAsync(
                    SecurityEventType.InvalidContentType,
                    clientIp,
                    context.Request.Path,
                    $"不支持的 Content-Type: {contentType}",
                    requestId);

                await WriteErrorResponse(context, 415, "Unsupported Media Type: Only application/json is supported", requestId);
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
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
            {
                { "exception.message", ex.Message },
                { "exception.type", ex.GetType().Name },
                { "exception.stacktrace", ex.StackTrace ?? string.Empty }
            }));
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
                await WriteErrorResponse(context, 400, "Bad Request: Invalid request format", requestId);
                return;
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

            // 步骤 5：验证签名
            if (!await ValidateRequestSignatureAsync(context, eventRequest, requestBody, encryptKey, clientIp, requestId, validator))
            {
                return;
            }

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
            await WriteErrorResponse(context, 400, "Bad Request: Invalid JSON format", requestId);
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
                await WriteErrorResponse(context, 400, "Bad Request: Failed to decrypt verification request", requestId);
                return;
            }

            // 解析为验证请求
            var verificationRequest = JsonSerializer.Deserialize(
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

            await WriteErrorResponse(context, 400, "Bad Request: Invalid verification request format", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理加密验证请求时发生错误");
            await WriteErrorResponse(context, 400, "Bad Request: Failed to process verification request", requestId);
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
    private async Task<bool> ValidateRequestSignatureAsync(HttpContext context, FeishuWebhookRequest eventRequest, string requestBody, string encryptKey, string clientIp, string requestId, IFeishuEventValidator validator)
    {
        var headerSignature = context.Request.Headers["X-Lark-Signature"].FirstOrDefault();
        if (!await validator.ValidateHeaderSignatureAsync(
            eventRequest.Timestamp,
            eventRequest.Nonce,
            requestBody,
            headerSignature,
            encryptKey))
        {
            _ = _securityAuditService?.LogSecurityFailureAsync(
                SecurityEventType.SignatureValidation,
                clientIp,
                context.Request.Path,
                "X-Lark-Signature 请求头签名验证失败",
                requestId);

            await WriteErrorResponse(context, 401, "Unauthorized: Invalid X-Lark-Signature", requestId);
            return false;
        }

        _ = _securityAuditService?.LogSecuritySuccessAsync(
            SecurityEventType.SignatureValidation,
            clientIp,
            context.Request.Path,
            "X-Lark-Signature 请求头签名验证成功",
            requestId);

        return true;
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
                                    new InvalidOperationException(result.ErrorReason ?? "Unknown error"),
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
                                ex,
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
