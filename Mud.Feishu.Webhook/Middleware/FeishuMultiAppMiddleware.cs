// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证 位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Interceptors;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Exceptions;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Serialization;
using Mud.Feishu.Webhook.Utils;
using System.Diagnostics;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书多应用 Webhook 中间件
/// </summary>
public class FeishuMultiAppMiddleware
{
    private static class FeishuWebhookActivitySource
    {
        public static readonly ActivitySource Source = new ActivitySource("Mud.Feishu.Webhook.MultiApp");
    }

    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeishuMultiAppMiddleware> _logger;
    private readonly IOptionsMonitor<FeishuWebhookOptions> _options;
    private readonly FeishuWebhookHandlerRegistry _handlerRegistry;
    private readonly FeishuWebhookInterceptorRegistry _interceptorRegistry;

    /// <summary>
    /// 获取当前配置选项（支持热更新）
    /// </summary>
    private FeishuWebhookOptions Options => _options.CurrentValue;

    public FeishuMultiAppMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<FeishuMultiAppMiddleware> logger,
        IOptionsMonitor<FeishuWebhookOptions> options,
        FeishuWebhookHandlerRegistry handlerRegistry,
        FeishuWebhookInterceptorRegistry interceptorRegistry)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options;
        _handlerRegistry = handlerRegistry;
        _interceptorRegistry = interceptorRegistry;

        // 监听配置变更
        _options.OnChange((newOptions, name) =>
        {
            var oldOptions = Options;
            var changes = new List<string>();

            // 检测关键配置项的变更
            if (oldOptions.GlobalRoutePrefix != newOptions.GlobalRoutePrefix)
            {
                changes.Add($"GlobalRoutePrefix: {oldOptions.GlobalRoutePrefix} → {newOptions.GlobalRoutePrefix}");
            }

            if (oldOptions.AutoRegisterEndpoint != newOptions.AutoRegisterEndpoint)
            {
                changes.Add($"AutoRegisterEndpoint: {oldOptions.AutoRegisterEndpoint} → {newOptions.AutoRegisterEndpoint}");
            }

            if (oldOptions.MaxRequestBodySize != newOptions.MaxRequestBodySize)
            {
                changes.Add($"MaxRequestBodySize: {oldOptions.MaxRequestBodySize} bytes → {newOptions.MaxRequestBodySize} bytes");
            }

            if (oldOptions.AllowedHttpMethods.Count != newOptions.AllowedHttpMethods.Count ||
                !oldOptions.AllowedHttpMethods.SetEquals(newOptions.AllowedHttpMethods))
            {
                changes.Add($"AllowedHttpMethods: [{string.Join(", ", oldOptions.AllowedHttpMethods)}] → [{string.Join(", ", newOptions.AllowedHttpMethods)}]");
            }

            // 检测 IP 白名单的变更
            if (!oldOptions.AllowedSourceIPs.SetEquals(newOptions.AllowedSourceIPs))
            {
                changes.Add($"AllowedSourceIPs: [{string.Join(", ", oldOptions.AllowedSourceIPs)}] → [{string.Join(", ", newOptions.AllowedSourceIPs)}]");
            }

            // 检测限流配置的变更
            if (oldOptions.RateLimit.EnableRateLimit != newOptions.RateLimit.EnableRateLimit)
            {
                changes.Add($"RateLimit.EnableRateLimit: {oldOptions.RateLimit.EnableRateLimit} → {newOptions.RateLimit.EnableRateLimit}");
            }

            if (oldOptions.RateLimit.WindowSizeSeconds != newOptions.RateLimit.WindowSizeSeconds)
            {
                changes.Add($"RateLimit.WindowSizeSeconds: {oldOptions.RateLimit.WindowSizeSeconds}s → {newOptions.RateLimit.WindowSizeSeconds}s");
            }

            if (oldOptions.RateLimit.MaxRequestsPerWindow != newOptions.RateLimit.MaxRequestsPerWindow)
            {
                changes.Add($"RateLimit.MaxRequestsPerWindow: {oldOptions.RateLimit.MaxRequestsPerWindow} → {newOptions.RateLimit.MaxRequestsPerWindow}");
            }

            if (changes.Count > 0)
            {
                _logger.LogInformation("飞书多应用 Webhook 配置已更新，来源: {ChangeSource}，变更内容:\n{Changes}", name, string.Join("\n  - ", changes));
            }
            else
            {
                _logger.LogDebug("飞书多应用 Webhook 配置已更新，来源: {ChangeSource}（无关键配置变更）", name);
            }
        });
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? string.Empty;

        // 尝试从路径中提取 AppKey
        var appKey = ExtractAppKeyFromPath(path);
        if (string.IsNullOrEmpty(appKey))
        {
            await _next(context);
            return;
        }

        // 验证应用是否存在
        if (!Options.Apps.ContainsKey(appKey ?? string.Empty))
        {
            _logger.LogWarning("未知的应用键: {AppKey}", appKey);
            await _next(context);
            return;
        }

        // 验证应用是否有处理器
        if (!_handlerRegistry.HasHandlers(appKey ?? string.Empty))
        {
            _logger.LogWarning("应用 {AppKey} 没有注册任何处理器", appKey);
            await _next(context);
            return;
        }

        // 获取应用配置
        var appConfig = Options.GetAppConfig(appKey ?? string.Empty)!;
        var requestId = RequestIdHelper.GetOrGenerateRequestId(context);
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        using var activity = FeishuWebhookActivitySource.Source.StartActivity("FeishuWebhook.MultiApp");
        activity?.SetTag("app_key", appKey);
        activity?.SetTag("request.id", requestId);
        activity?.SetTag("request.path", path);
        activity?.SetTag("request.client_ip", clientIp);

        // 使用日志作用域自动注入 AppKey 和 RequestId
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["AppKey"] = appKey ?? "unknown",
            ["RequestId"] = requestId,
            ["ClientIp"] = clientIp,
            ["Path"] = path
        });

        try
        {
            // 验证 HTTP 方法
            if (context.Request.Method != "POST")
            {
                await WriteErrorResponse(context, 405, "Method Not Allowed", requestId);
                return;
            }

            // 验证 Content-Type
            var contentType = context.Request.ContentType;
            if (string.IsNullOrEmpty(contentType) || !contentType.ToLowerInvariant().Contains("application/json"))
            {
                await WriteErrorResponse(context, 400, "Unsupported Media Type", requestId);
                return;
            }

            // 读取请求体
            var requestBody = await ReadRequestBodyAsync(context.Request);
            if (string.IsNullOrEmpty(requestBody))
            {
                await WriteErrorResponse(context, 400, "Bad Request: Empty body", requestId);
                return;
            }

            _logger.LogInformation("收到应用的 Webhook 请求");

            // 处理请求
            await ProcessWebhookRequestAsync(
                context,
                requestBody,
                requestId,
                clientIp,
                appKey ?? string.Empty);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "处理应用的 Webhook 请求时发生错误");
            await WriteErrorResponse(context, 500, "Internal Server Error", requestId);
        }
        finally
        {
            stopwatch.Stop();
            activity?.SetTag("request.duration_ms", stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// 从路径中提取 AppKey
    /// </summary>
    /// <example>
    /// /feishu/app1 -> app1
    /// /feishu/app2/events -> app2
    /// </example>
    private string? ExtractAppKeyFromPath(string path)
    {
        var globalPrefix = $"/{Options.GlobalRoutePrefix}";

        if (!path.StartsWith(globalPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var remainingPath = path.Substring(globalPrefix.Length).Trim('/');
        var segments = remainingPath.Split('/');

        if (segments.Length > 0 && !string.IsNullOrEmpty(segments[0]))
        {
            return segments[0];
        }

        return null;
    }

    /// <summary>
    /// 处理 Webhook 请求
    /// </summary>
    private async Task ProcessWebhookRequestAsync(
        HttpContext context,
        string requestBody,
        string requestId,
        string clientIp,
        string appKey)
    {
        using var scope = _scopeFactory.CreateScope();
        var webhookService = scope.ServiceProvider.GetRequiredService<IFeishuWebhookService>();

        // 设置当前应用键以支持多应用场景
        webhookService.SetCurrentAppKey(appKey);

        try
        {
            // 尝试处理明文 URL 验证请求
            if (await TryHandlePlaintextVerificationAsync(context, requestBody, webhookService))
            {
                return;
            }

            // 解析加密请求
            var eventRequest = JsonSerializer.Deserialize(
                requestBody,
                FeishuJsonContext.Default.FeishuWebhookRequest);

            if (eventRequest == null || string.IsNullOrEmpty(eventRequest.Encrypt))
            {
                await WriteErrorResponse(context, 400, "Bad Request: Missing encrypt field", requestId);
                return;
            }

            // 解密请求数据
            var decryptedData = await webhookService.DecryptEventAsync(eventRequest.Encrypt!);

            if (decryptedData == null)
            {
                _logger.LogError("解密失败");
                await WriteErrorResponse(context, 400, "Bad Request: Decryption failed", requestId);
                return;
            }

            _logger.LogInformation("解密成功 - EventType: {EventType}, EventId: {EventId}, DecryptedAppId: {DecryptedAppId}",
                decryptedData.EventType ?? "(null)",
                decryptedData.EventId ?? "(null)",
                decryptedData.AppId ?? "(null)");

            // 检查是否为加密验证请求
            if (decryptedData.EventType == "url_verification" ||
                (string.IsNullOrEmpty(decryptedData.EventType) && string.IsNullOrEmpty(decryptedData.EventId)))
            {
                await HandleEncryptedVerificationAsync(context, decryptedData);
                return;
            }

            // 验证签名
            await ValidateRequestSignatureAsync(
                context,
                eventRequest,
                clientIp,
                requestId,
                appKey);

            // 处理事件请求
            await HandleEventRequestAsync(
                context,
                decryptedData,
                requestId,
                appKey,
                webhookService);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "反序列化请求体时发生错误, RequestId: {RequestId}", requestId);
            await WriteErrorResponse(context, 400, "Bad Request: Invalid JSON", requestId);
        }
    }

    /// <summary>
    /// 尝试处理明文 URL 验证请求
    /// </summary>
    private async Task<bool> TryHandlePlaintextVerificationAsync(
        HttpContext context,
        string requestBody,
        IFeishuWebhookService webhookService)
    {
        var verificationRequest = JsonSerializer.Deserialize(
            requestBody,
            FeishuJsonContext.Default.EventVerificationRequest);

        if (verificationRequest?.Type == "url_verification")
        {
            _logger.LogDebug("检测到明文 URL 验证请求");

            var verificationResponse = await webhookService.VerifyEventSubscriptionAsync(verificationRequest);

            if (verificationResponse == null)
            {
                _logger.LogWarning("验证令牌不匹配或验证失败");
                return false;
            }

            _logger.LogInformation("明文验证成功，返回挑战码");
            await WriteJsonResponse(context, 200, verificationResponse);
            return true;
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
    private async Task ValidateRequestSignatureAsync(
        HttpContext context,
        FeishuWebhookRequest eventRequest,
        string clientIp,
        string requestId,
        string appKey)
    {
        // 构造请求对象用于签名验证
        var webhookRequest = new FeishuWebhookRequest
        {
            Encrypt = eventRequest.Encrypt,
            Signature = context.Request.Headers["X-Lark-Signature"].FirstOrDefault() ?? string.Empty,
            Nonce = context.Request.Headers["X-Lark-Request-Nonce"].FirstOrDefault() ?? string.Empty,
            Timestamp = long.TryParse(context.Request.Headers["X-Lark-Request-Timestamp"].FirstOrDefault(), out var ts) ? ts : 0
        };

        using var scope = _scopeFactory.CreateScope();
        var webhookService = scope.ServiceProvider.GetRequiredService<IFeishuWebhookService>();
        webhookService.SetCurrentAppKey(appKey);

        if (!await webhookService.ValidateRequestSignature(webhookRequest))
        {
            throw new FeishuWebhookSecurityException(
                "X-Lark-Signature 请求头签名验证失败",
                clientIp: clientIp,
                requestId: requestId);
        }

        _logger.LogInformation("签名验证成功，AppKey: {AppKey}", appKey);
    }

    /// <summary>
    /// 处理事件请求
    /// </summary>
    private async Task HandleEventRequestAsync(
        HttpContext context,
        EventData eventData,
        string requestId,
        string appKey,
        IFeishuWebhookService webhookService)
    {
        try
        {
            // 使用 Webhook 服务处理事件
            var result = await webhookService.HandleEventAsync(eventData);

            if (!result.Success)
            {
                _logger.LogError("事件处理失败: {EventType}, 事件ID: {EventId}, 原因: {Reason}",
                    eventData.EventType, eventData.EventId, result.ErrorReason ?? "未知错误");
                await WriteErrorResponse(context, 500, "Internal Server Error", requestId);
                return;
            }

            _logger.LogInformation("事件处理完成: {EventType}, 事件ID: {EventId}, AppKey: {AppKey}",
                eventData.EventType, eventData.EventId, appKey);

            await WriteJsonResponse(context, 200, new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理事件时发生错误");
            await WriteErrorResponse(context, 500, "Internal Server Error", requestId);
        }
    }

    /// <summary>
    /// 读取请求体
    /// </summary>
    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Position = 0;
        return await new StreamReader(request.Body).ReadToEndAsync();
    }

    /// <summary>
    /// 写入 JSON 响应
    /// </summary>
    private async Task WriteJsonResponse<T>(HttpContext context, int statusCode, T data)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

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
