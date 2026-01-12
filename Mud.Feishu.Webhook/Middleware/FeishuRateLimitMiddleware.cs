// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Services;
using System.Collections.Concurrent;

namespace Mud.Feishu.Webhook.Middleware;

/// <summary>
/// 飞书 Webhook 请求频率限制中间件
/// </summary>
public class FeishuRateLimitMiddleware(
    RequestDelegate next,
    IOptions<FeishuWebhookOptions> webhookOptions,
    IOptions<RateLimitOptions> rateLimitOptions,
    ILogger<FeishuRateLimitMiddleware> logger,
    ISecurityAuditService? securityAuditService,
    IThreatDetectionService? threatDetectionService = null)
{
    private readonly RequestDelegate _next = next;
    private readonly FeishuWebhookOptions _webhookOptions = webhookOptions.Value;
    private readonly RateLimitOptions _rateLimitOptions = rateLimitOptions.Value;
    private readonly ILogger<FeishuRateLimitMiddleware> _logger = logger;
    private readonly ISecurityAuditService? _securityAuditService = securityAuditService;
    private readonly IThreatDetectionService? _threatDetectionService = threatDetectionService;

    // 使用并发字典和滑动窗口计数器：ConcurrentDictionary<IP, (Count, WindowStart)>
    private readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _requestCounts = new();

    /// <summary>
    /// 处理 HTTP 请求
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // 如果未启用限流，直接放行
        if (!_rateLimitOptions.EnableRateLimit)
        {
            await _next(context);
            return;
        }

        // 检查是否为 Webhook 请求
        if (!context.Request.Path.StartsWithSegments($"/{_webhookOptions.RoutePrefix}", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // 获取客户端 IP
        var clientIp = GetClientIp(context);
        if (string.IsNullOrEmpty(clientIp))
        {
            _logger.LogWarning("无法获取客户端 IP，拒绝请求");
            await WriteTooManyRequestsResponse(context, "无法识别客户端 IP");
            return;
        }

        // 检查是否在白名单中
        if (_rateLimitOptions.WhitelistIPs.Contains(clientIp))
        {
            _logger.LogDebug("客户端 IP {ClientIP} 在白名单中，跳过限流", clientIp);
            await _next(context);
            return;
        }

        var now = DateTime.UtcNow;

        // 清理过期的窗口记录（超过窗口大小）
        CleanupExpiredWindows(now);

        // 获取或创建计数器
        if (_requestCounts.TryGetValue(clientIp, out var counter))
        {
            // 检查是否超出时间窗口
            if ((now - counter.WindowStart).TotalSeconds > _rateLimitOptions.WindowSizeSeconds)
            {
                // 新窗口，重置计数
                _requestCounts[clientIp] = (1, now);
            }
            else
            {
                // 同一窗口，增加计数
                if (counter.Count >= _rateLimitOptions.MaxRequestsPerWindow)
                {
                    _logger.LogWarning("客户端 IP {ClientIP} 请求频率超出限制：{Count}/{MaxRequests} 在 {WindowSize}秒内",
                        clientIp, counter.Count, _rateLimitOptions.MaxRequestsPerWindow, _rateLimitOptions.WindowSizeSeconds);

                    // 记录安全审计日志
                    _ = _securityAuditService?.LogSecurityFailureAsync(
                        SecurityEventType.RateLimitExceeded,
                        clientIp,
                        context.Request.Path,
                        $"请求频率超出限制：{counter.Count}/{_rateLimitOptions.MaxRequestsPerWindow} 在 {_rateLimitOptions.WindowSizeSeconds}秒内",
                        context.Items["RequestId"]?.ToString());

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(_rateLimitOptions.WindowSizeSeconds * 1000);
                        // 重新检查是否仍然不存在，然后添加新的计数器
                        if (!_requestCounts.ContainsKey(clientIp))
                        {
                            _requestCounts.TryAdd(clientIp, (1, DateTime.UtcNow));
                        }
                    });

                    await WriteTooManyRequestsResponse(context,
                        $"{_rateLimitOptions.TooManyRequestsMessage}，请在 {_rateLimitOptions.WindowSizeSeconds} 秒后重试");
                    return;
                }

                // 使用 CompareExchange 确保原子性更新
                var newCounter = (counter.Count + 1, counter.WindowStart);
                while (!_requestCounts.TryUpdate(clientIp, newCounter, counter))
                {
                    // 如果更新失败，重新获取当前值
                    if (!_requestCounts.TryGetValue(clientIp, out counter))
                    {
                        // 如果键不存在，添加新条目
                        _requestCounts.TryAdd(clientIp, (1, now));
                        break;
                    }
                    newCounter = (counter.Count + 1, counter.WindowStart);
                }
            }
        }
        else
        {
            // 新 IP，尝试添加计数器，如果已存在则获取现有值并递增
            _requestCounts.TryAdd(clientIp, (1, now));
        }

        await _next(context);
    }

    /// <summary>
    /// 获取客户端真实 IP
    /// </summary>
    private static string? GetClientIp(HttpContext context)
    {
        // 优先检查代理头
        var headers = new[] { "X-Forwarded-For", "X-Real-IP", "CF-Connecting-IP" };

        foreach (var header in headers)
        {
            if (context.Request.Headers.TryGetValue(header, out var values))
            {
                var ip = values.FirstOrDefault()?.Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(ip) && ip != "::1" && ip != "127.0.0.1")
                {
                    return ip;
                }
            }
        }

        // 回退到直接连接 IP
        return context.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// 清理过期的窗口记录
    /// </summary>
    private void CleanupExpiredWindows(DateTime now)
    {
        var expiredKeys = _requestCounts
            .Where(kvp => (now - kvp.Value.WindowStart).TotalSeconds > _rateLimitOptions.WindowSizeSeconds)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _requestCounts.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// 写入 429 响应
    /// </summary>
    private async Task WriteTooManyRequestsResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = _rateLimitOptions.TooManyRequestsStatusCode;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            success = false,
            error = new
            {
                code = context.Response.StatusCode,
                message
            }
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
    }
}
