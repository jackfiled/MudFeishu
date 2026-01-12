// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Services;

namespace Mud.Feishu.Webhook.Health;

/// <summary>
/// 飞书 Webhook 健康检查
/// </summary>
public class FeishuWebhookHealthCheck : IHealthCheck
{
    private readonly IOptionsMonitor<FeishuWebhookOptions> _options;
    private readonly FeishuWebhookConcurrencyService _concurrencyService;
    private readonly MetricsCollector _metrics;

    /// <inheritdoc />
    public FeishuWebhookHealthCheck(
        IOptionsMonitor<FeishuWebhookOptions> options,
        FeishuWebhookConcurrencyService concurrencyService,
        MetricsCollector metrics)
    {
        _options = options;
        _concurrencyService = concurrencyService;
        _metrics = metrics;
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        var counters = _metrics.GetAllCounters();

        counters.TryGetValue("successful_events", out var successfulEvents);
        counters.TryGetValue("failed_events", out var failedEvents);
        var totalEvents = successfulEvents + failedEvents;

        counters.TryGetValue("cancelled_events", out var cancelledEvents);
        counters.TryGetValue("signature_validation_failures", out var signatureFailures);
        counters.TryGetValue("decryption_failures", out var decryptionFailures);

        var healthData = new Dictionary<string, object>
        {
            ["configuration_valid"] = true,
            ["max_concurrent_events"] = options.MaxConcurrentEvents,
            ["timeout_ms"] = options.EventHandlingTimeoutMs,
            ["successful_events"] = successfulEvents,
            ["failed_events"] = failedEvents,
            ["cancelled_events"] = cancelledEvents,
            ["signature_failures"] = signatureFailures,
            ["decryption_failures"] = decryptionFailures,
            ["total_events"] = totalEvents,
            ["available_concurrent_slots"] = _concurrencyService.AvailableCount,
            ["failure_rate_threshold_unhealthy"] = options.HealthCheckUnhealthyFailureRateThreshold,
            ["failure_rate_threshold_degraded"] = options.HealthCheckDegradedFailureRateThreshold
        };

        // 计算失败率
        double failureRate = totalEvents > 0 ? (double)failedEvents / totalEvents : 0;
        healthData["failure_rate"] = failureRate;

        // 使用配置的阈值判断健康状态
        var minEvents = options.HealthCheckMinEventsThreshold;
        var unhealthyThreshold = options.HealthCheckUnhealthyFailureRateThreshold;
        var degradedThreshold = options.HealthCheckDegradedFailureRateThreshold;

        // 如果失败率超过不健康阈值，返回不健康状态
        if (failureRate > unhealthyThreshold && totalEvents >= minEvents)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"事件处理失败率过高：{failureRate:P2} (阈值: {unhealthyThreshold:P2})",
                null,
                healthData));
        }

        // 如果失败率超过降级阈值，返回降级状态
        if (failureRate > degradedThreshold && totalEvents >= minEvents)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"事件处理失败率略高：{failureRate:P2} (阈值: {degradedThreshold:P2})，建议检查",
                null,
                healthData));
        }

        // 返回健康状态
        return Task.FromResult(HealthCheckResult.Healthy(
            $"飞书 Webhook 服务运行正常 (失败率: {failureRate:P2})",
            healthData));
    }
}