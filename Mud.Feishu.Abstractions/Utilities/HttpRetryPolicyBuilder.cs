// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Polly;
using Polly.Retry;
using System.Net;

namespace Mud.Feishu.Abstractions.Utilities;

/// <summary>
/// HTTP 重试策略构建器
/// </summary>
internal static class HttpRetryPolicyBuilder
{
    /// <summary>
    /// 构建智能重试策略，区分可重试错误（5xx、网络异常）和不可重试错误（4xx）
    /// </summary>
    /// <param name="retryCount">最大重试次数</param>
    /// <param name="retryDelayMs">基础重试延迟（毫秒）</param>
    /// <returns>异步重试策略</returns>
    /// <remarks>
    /// 重试策略规则：
    /// <list type="bullet">
    ///   <item><description>5xx 服务器错误：重试</description></item>
    ///   <item><description>408 Request Timeout：重试</description></item>
    ///   <item><description>429 Too Many Requests：重试</description></item>
    ///   <item><description>网络异常（连接失败、DNS 解析失败等）：重试</description></item>
    ///   <item><description>4xx 客户端错误（除 408、429）：不重试</description></item>
    /// </list>
    /// </remarks>
    public static AsyncRetryPolicy<HttpResponseMessage> BuildRetryPolicy(int retryCount, int retryDelayMs)
    {
        return Policy
            .HandleResult<HttpResponseMessage>(response =>
            {
                // 检查响应状态码
                var statusCode = (int)response.StatusCode;

                // 可重试的 4xx 错误
                if (statusCode == 408) // Request Timeout
                    return true;

                if (statusCode == 429) // Too Many Requests
                    return true;

                // 可重试的 5xx 服务器错误
                if (statusCode >= 500 && statusCode < 600)
                    return true;

                // 其他 4xx 错误不重试
                if (statusCode >= 400 && statusCode < 500)
                    return false;

                // 2xx 和 3xx 成功响应不重试
                if (statusCode >= 200 && statusCode < 400)
                    return false;

                // 1xx 和其他状态码重试
                return true;
            })
            .Or<HttpRequestException>(ex =>
            {
                // 网络异常总是重试
                return true;
            })
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => CalculateRetryDelay(retryAttempt, retryDelayMs));
    }

    /// <summary>
    /// 计算重试延迟（指数退避策略）
    /// </summary>
    /// <param name="retryAttempt">当前重试次数（从 1 开始）</param>
    /// <param name="baseDelayMs">基础延迟（毫秒）</param>
    /// <returns>实际延迟时间</returns>
    /// <remarks>
    /// 延迟计算公式：baseDelayMs * 2^(retryAttempt - 1) * (0.8 + Random() * 0.4)
    /// <para>
    /// 指数退避：每次重试延迟翻倍
    /// 抖动：加入随机因子（±20%），避免多个客户端同时重试导致惊群效应
    /// </para>
    /// </remarks>
    private static TimeSpan CalculateRetryDelay(int retryAttempt, int baseDelayMs)
    {
        // 指数退避：baseDelayMs * 2^(retryAttempt - 1)
        var exponentialDelay = baseDelayMs * Math.Pow(2, retryAttempt - 1);

        // 添加抖动（jitter），避免多个客户端同时重试
        var random = new Random();
        var jitterFactor = 0.8 + (random.NextDouble() * 0.4); // 0.8 ~ 1.2

        var finalDelayMs = (int)(exponentialDelay * jitterFactor);

        // 最大延迟限制为 30 秒
        const int maxDelayMs = 30000;
        finalDelayMs = Math.Min(finalDelayMs, maxDelayMs);

        return TimeSpan.FromMilliseconds(finalDelayMs);
    }
}
