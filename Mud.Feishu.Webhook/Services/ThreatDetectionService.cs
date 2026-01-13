// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 威胁检测服务实现
/// </summary>
public class ThreatDetectionService : IThreatDetectionService
{
    private readonly ILogger<ThreatDetectionService> _logger;
    private readonly IOptionsMonitor<FeishuWebhookOptions> _optionsMonitor;

    // 使用并发字典存储IP行为模式
    private readonly ConcurrentDictionary<string, IpBehavior> _ipBehaviors = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public ThreatDetectionService(
        ILogger<ThreatDetectionService> logger,
        IOptionsMonitor<FeishuWebhookOptions> optionsMonitor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    }

    /// <inheritdoc />
    public async Task<ThreatDetectionResult> AnalyzeRequestAsync(
        string clientIp,
        string requestPath,
        string requestMethod,
        IHeaderDictionary requestHeaders,
        string requestBody,
        string? requestId = null)
    {
        var result = new ThreatDetectionResult
        {
            IsThreat = false,
            ThreatLevel = 0,
            ThreatType = ThreatType.None,
            Description = string.Empty,
            RecommendedAction = ThreatAction.Allow
        };

        // 检查恶意内容
        var maliciousContentResult = CheckForMaliciousContent(requestBody);
        if (maliciousContentResult.IsThreat)
        {
            return maliciousContentResult;
        }

        // 检查异常访问模式
        var accessPatternResult = await CheckAccessPatternAsync(clientIp, requestPath, requestMethod);
        if (accessPatternResult.IsThreat)
        {
            return accessPatternResult;
        }

        // 检查请求头中的异常
        var headerResult = CheckRequestHeaders(requestHeaders);
        if (headerResult.IsThreat)
        {
            return headerResult;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task RecordRequestOutcomeAsync(
        string clientIp,
        string requestPath,
        bool success,
        long responseTimeMs)
    {
        var behavior = _ipBehaviors.GetOrAdd(clientIp, _ => new IpBehavior());

        // 更新行为统计
        behavior.UpdateStats(success, responseTimeMs);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 检查请求体中是否包含恶意内容
    /// </summary>
    private ThreatDetectionResult CheckForMaliciousContent(string requestBody)
    {
        var result = new ThreatDetectionResult
        {
            IsThreat = false,
            ThreatLevel = 0,
            ThreatType = ThreatType.None,
            Description = string.Empty,
            RecommendedAction = ThreatAction.Allow
        };

        if (string.IsNullOrEmpty(requestBody))
        {
            return result;
        }

        // 检查潜在的恶意模式
        var maliciousPatterns = new[]
        {
            @"<script[^>]*>.*?</script>", // XSS脚本
            @"<iframe[^>]*>.*?</iframe>", // iframe注入
            @"eval\s*\(", // JavaScript eval
            @"javascript:", // JavaScript协议
            @"vbscript:", // VBScript协议
            @"on\w+\s*=", // 事件处理器
            @"<object[^>]*>.*?</object>", // object标签
            @"<embed[^>]*>.*?</embed>", // embed标签
        };

        foreach (var pattern in maliciousPatterns)
        {
            if (Regex.IsMatch(requestBody, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                result.IsThreat = true;
                result.ThreatLevel = 4; // 高威胁
                result.ThreatType = ThreatType.MaliciousContent;
                result.Description = $"检测到潜在恶意内容模式: {pattern}";
                result.RecommendedAction = ThreatAction.Block;
                _logger.LogWarning("威胁检测: 在请求体中检测到恶意内容模式 {Pattern}", pattern);
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// 检查访问模式异常
    /// </summary>
    private async Task<ThreatDetectionResult> CheckAccessPatternAsync(
        string clientIp,
        string requestPath,
        string requestMethod)
    {
        var result = new ThreatDetectionResult
        {
            IsThreat = false,
            ThreatLevel = 0,
            ThreatType = ThreatType.None,
            Description = string.Empty,
            RecommendedAction = ThreatAction.Allow
        };

        var behavior = _ipBehaviors.GetOrAdd(clientIp, _ => new IpBehavior());

        // 检查频率异常
        var recentRequests = behavior.GetRecentRequests(TimeSpan.FromMinutes(1));
        if (recentRequests > 50) // 1分钟内超过50次请求
        {
            result.IsThreat = true;
            result.ThreatLevel = 3; // 中高威胁
            result.ThreatType = ThreatType.FrequencyAnomaly;
            result.Description = $"IP {clientIp} 在1分钟内发送了 {recentRequests} 个请求，超出正常范围";
            result.RecommendedAction = ThreatAction.RateLimit;
            _logger.LogWarning("威胁检测: 检测到频率异常，IP {ClientIp} 在1分钟内发送了 {Count} 个请求", clientIp, recentRequests);
            return result;
        }

        // 检查路径访问异常
        var pathAccessCount = behavior.GetPathAccessCount(requestPath, TimeSpan.FromMinutes(5));
        if (pathAccessCount > 20) // 5分钟内访问同一路径超过20次
        {
            result.IsThreat = true;
            result.ThreatLevel = 2; // 中等威胁
            result.ThreatType = ThreatType.AnomalousAccessPattern;
            result.Description = $"IP {clientIp} 在5分钟内访问路径 {requestPath} {pathAccessCount} 次";
            result.RecommendedAction = ThreatAction.RateLimit;
            _logger.LogWarning("威胁检测: 检测到路径访问异常，IP {ClientIp} 在5分钟内访问 {Path} {Count} 次", clientIp, requestPath, pathAccessCount);
            return result;
        }

        // 记录本次请求
        behavior.RecordRequest(requestPath);

        return result;
    }

    /// <summary>
    /// 检查请求头中的异常
    /// </summary>
    private ThreatDetectionResult CheckRequestHeaders(IHeaderDictionary requestHeaders)
    {
        var result = new ThreatDetectionResult
        {
            IsThreat = false,
            ThreatLevel = 0,
            ThreatType = ThreatType.None,
            Description = string.Empty,
            RecommendedAction = ThreatAction.Allow
        };

        // 检查 User-Agent 中的异常模式
        var userAgent = requestHeaders["User-Agent"].FirstOrDefault();
        if (!string.IsNullOrEmpty(userAgent))
        {
            var suspiciousUserAgents = new[]
            {
                "sqlmap",
                "nmap",
                "nessus",
                "nikto",
                "dirbuster",
                "hydra",
                "medusa"
            };

            foreach (var suspiciousAgent in suspiciousUserAgents)
            {
                if (userAgent.IndexOf(suspiciousAgent, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.IsThreat = true;
                    result.ThreatLevel = 3; // 中高威胁
                    result.ThreatType = ThreatType.AnomalousAccessPattern;
                    result.Description = $"检测到可疑的 User-Agent: {userAgent}";
                    result.RecommendedAction = ThreatAction.Log;
                    _logger.LogWarning("威胁检测: 检测到可疑 User-Agent {UserAgent}", userAgent);
                    return result;
                }
            }
        }

        // 检查 Content-Type 异常
        var contentType = requestHeaders["Content-Type"].FirstOrDefault();
        if (!string.IsNullOrEmpty(contentType) &&
            contentType.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) < 0)
        {
            // 非JSON内容类型可能需要关注
            result.IsThreat = false; // 不直接标记为威胁，但记录日志
            result.ThreatLevel = 1; // 低威胁
            result.ThreatType = ThreatType.AnomalousAccessPattern;
            result.Description = $"非标准 Content-Type: {contentType}";
            result.RecommendedAction = ThreatAction.Log;
            _logger.LogDebug("威胁检测: 检测到非标准 Content-Type {ContentType}", contentType);
        }

        return result;
    }
}

/// <summary>
/// IP行为模式
/// </summary>
internal class IpBehavior
{
    private readonly ConcurrentQueue<DateTimeOffset> _requestTimestamps = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTimeOffset>> _pathAccessTimes = new();
    private readonly object _lock = new();

    /// <summary>
    /// 记录请求
    /// </summary>
    public void RecordRequest(string path)
    {
        var now = DateTimeOffset.UtcNow;

        // 记录总请求时间
        _requestTimestamps.Enqueue(now);

        // 记录特定路径访问时间
        var pathQueue = _pathAccessTimes.GetOrAdd(path, _ => new ConcurrentQueue<DateTimeOffset>());
        pathQueue.Enqueue(now);

        // 清理超过时间窗口的记录
        CleanupOldRecords();
    }

    /// <summary>
    /// 获取最近的请求数量
    /// </summary>
    public int GetRecentRequests(TimeSpan timeWindow)
    {
        var cutoffTime = DateTimeOffset.UtcNow - timeWindow;
        var recentRequests = _requestTimestamps.Where(t => t >= cutoffTime).Count();
        return recentRequests;
    }

    /// <summary>
    /// 获取路径访问次数
    /// </summary>
    public int GetPathAccessCount(string path, TimeSpan timeWindow)
    {
        var cutoffTime = DateTimeOffset.UtcNow - timeWindow;
        if (_pathAccessTimes.TryGetValue(path, out var pathQueue))
        {
            return pathQueue.Where(t => t >= cutoffTime).Count();
        }
        return 0;
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    public void UpdateStats(bool success, long responseTimeMs)
    {
        // 这里可以实现更复杂的统计逻辑
    }

    /// <summary>
    /// 清理过期记录
    /// </summary>
    private void CleanupOldRecords()
    {
        var cutoffTime = DateTimeOffset.UtcNow.AddMinutes(-10); // 保留10分钟内的记录

        // 清理总请求时间队列
        while (_requestTimestamps.TryPeek(out var oldest) && oldest < cutoffTime)
        {
            _requestTimestamps.TryDequeue(out _);
        }

        // 清理各路径的访问时间队列
        foreach (var pathQueue in _pathAccessTimes.Values)
        {
            while (pathQueue.TryPeek(out var pathOldest) && pathOldest < cutoffTime)
            {
                pathQueue.TryDequeue(out _);
            }
        }
    }
}