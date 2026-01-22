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
            _logger.LogInformation("飞书多应用 Webhook 配置已更新，来源: {ChangeSource}", name);
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
                appKey ?? string.Empty,
                appConfig);
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
        string appKey,
        FeishuAppWebhookOptions appConfig)
    {
        using var scope = _scopeFactory.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IFeishuEventValidator>() as FeishuEventValidator;
        var decryptor = scope.ServiceProvider.GetRequiredService<IFeishuEventDecryptor>();

        // 设置当前应用键以支持多应用去重
        if (validator != null)
        {
            validator.SetCurrentAppKey(appKey);
        }

        try
        {
            // 尝试处理明文 URL 验证请求
            if (await TryHandlePlaintextVerificationAsync(context, requestBody, appConfig, appKey))
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
            var decryptedData = await decryptor.DecryptAsync(eventRequest.Encrypt!, appConfig.EncryptKey);

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
                await HandleEncryptedVerificationAsync(context, decryptedData, appKey);
                return;
            }

        // 验证签名
        await ValidateRequestSignatureAsync(
            context,
            requestBody,
            appConfig.EncryptKey,
            clientIp,
            requestId,
            appKey ?? string.Empty,
            validator);

            // 处理事件请求
            await HandleEventRequestAsync(
                context,
                decryptedData,
                requestId,
                appKey ?? string.Empty,
                scope);
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
        FeishuAppWebhookOptions appConfig,
        string appKey)
    {
        var verificationRequest = JsonSerializer.Deserialize(
            requestBody,
            FeishuJsonContext.Default.EventVerificationRequest);

        if (verificationRequest?.Type == "url_verification")
        {
            _logger.LogDebug("检测到明文 URL 验证请求");
            
            if (verificationRequest.Token != appConfig.VerificationToken)
            {
                _logger.LogWarning("验证令牌不匹配");
                return false;
            }

            var verificationResponse = new EventVerificationResponse
            {
                Challenge = verificationRequest.Challenge ?? string.Empty
            };

            _logger.LogInformation("明文验证成功，返回挑战码，AppKey: {AppKey}", appKey);
            await WriteJsonResponse(context, 200, verificationResponse);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 处理加密的 URL 验证请求
    /// </summary>
    private async Task HandleEncryptedVerificationAsync(HttpContext context, EventData decryptedData, string appKey)
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

        _logger.LogInformation("加密验证成功，返回挑战码，AppKey: {AppKey}", appKey);
        await WriteJsonResponse(context, 200, verificationResponse);
    }

    /// <summary>
    /// 验证请求签名
    /// </summary>
    private async Task ValidateRequestSignatureAsync(
        HttpContext context,
        string requestBody,
        string encryptKey,
        string clientIp,
        string requestId,
        string appKey,
        IFeishuEventValidator validator)
    {
        var headerSignature = context.Request.Headers["X-Lark-Signature"].FirstOrDefault();
        var headerTimestamp = context.Request.Headers["X-Lark-Request-Timestamp"].FirstOrDefault();
        var headerNonce = context.Request.Headers["X-Lark-Request-Nonce"].FirstOrDefault();

        long timestamp = 0;
        if (!string.IsNullOrEmpty(headerTimestamp) && long.TryParse(headerTimestamp, out var parsedTimestamp))
        {
            timestamp = parsedTimestamp;
        }

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
        IServiceScope scope)
    {
        var handlerTypes = _handlerRegistry.GetHandlers(appKey);
        var interceptorTypes = _interceptorRegistry.GetInterceptors(appKey);

        // 获取处理器
        var handlers = new List<IFeishuEventHandler>();
        foreach (var handlerType in handlerTypes)
        {
            var handler = scope.ServiceProvider.GetService(handlerType) as IFeishuEventHandler;
            if (handler != null)
            {
                handlers.Add(handler);
            }
        }

        // 获取拦截器
        var interceptors = new List<IFeishuEventInterceptor>();
        foreach (var interceptorType in interceptorTypes)
        {
            var interceptor = scope.ServiceProvider.GetService(interceptorType) as IFeishuEventInterceptor;
            if (interceptor != null)
            {
                interceptors.Add(interceptor);
            }
        }

        try
        {
            // 前置拦截器
            foreach (var interceptor in interceptors)
            {
                var shouldContinue = await interceptor.BeforeHandleAsync(eventData.EventType, eventData);
                if (!shouldContinue)
                {
                    _logger.LogWarning("事件被拦截器中断: {EventType}, EventId: {EventId}",
                        eventData.EventType, eventData.EventId);
                    await WriteJsonResponse(context, 200, new { });
                    return;
                }
            }

            // 分发事件到处理器
            var eventType = eventData.EventType;
            var matchedHandlers = handlers.Where(h => h.SupportedEventType == eventType).ToList();

            if (matchedHandlers.Count == 0)
            {
                _logger.LogWarning("未找到事件类型 {EventType} 的处理器", eventType);
                await WriteJsonResponse(context, 200, new { });
                return;
            }

            // 并行处理
            var tasks = matchedHandlers.Select(h => h.HandleAsync(eventData));
            await Task.WhenAll(tasks);

            _logger.LogInformation("事件处理完成: {EventType}, 事件ID: {EventId}, AppKey: {AppKey}",
                eventData.EventType, eventData.EventId, appKey);

            // 后置拦截器
            foreach (var interceptor in interceptors)
            {
                await interceptor.AfterHandleAsync(eventData.EventType, eventData, null);
            }

            await WriteJsonResponse(context, 200, new { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理事件时发生错误");

            // 后置拦截器（异常情况）
            foreach (var interceptor in interceptors)
            {
                await interceptor.AfterHandleAsync(eventData.EventType, eventData, ex);
            }

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
