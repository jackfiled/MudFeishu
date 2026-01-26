// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Polly;
using Polly.Retry;

namespace Mud.Feishu.Abstractions.Utilities;

/// <summary>
/// HTTP 重试策略构建器
/// </summary>
internal static class HttpRetryPolicyBuilder
{
    // 静态 Random 实例，避免多线程下种子重复问题
    private static readonly Random JitterRandom = new Random();

    /// <summary>
    /// 构建智能重试策略，区分可重试错误（5xx、网络异常）和不可重试错误（4xx）
    /// </summary>
    /// <param name="retryCount">最大重试次数（必须 >= 0）</param>
    /// <param name="retryDelayMs">基础重试延迟（毫秒，必须 > 0）</param>
    /// <returns>异步重试策略</returns>
    /// <exception cref="ArgumentOutOfRangeException">当参数无效时抛出</exception>
    /// <remarks>
    /// 重试策略规则：
    /// <list type="bullet">
    ///   <item><description>5xx 服务器错误：重试</description></item>
    ///   <item><description>408 Request Timeout：重试</description></item>
    ///   <item><description>429 Too Many Requests：重试</description></item>
    ///   <item><description>网络异常（如 HttpRequestException、TimeoutException）：重试</description></item>
    ///   <item><description>其他 4xx 客户端错误：不重试</description></item>
    ///   <item><description>1xx、2xx、3xx 响应：不重试（成功或信息响应）</description></item>
    ///   <item><description>未知状态码（如 600+）：视为可重试（保守策略）</description></item>
    /// </list>
    /// </remarks>
    public static AsyncRetryPolicy<HttpResponseMessage> BuildRetryPolicy(int retryCount, int retryDelayMs)
    {
        if (retryCount < 0)
            throw new ArgumentOutOfRangeException(nameof(retryCount), "重试次数不能小于 0。");
        if (retryDelayMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(retryDelayMs), "基础延迟必须大于 0 毫秒。");

        return Policy
            .HandleResult<HttpResponseMessage>(IsRetriableResponse)
            .Or<HttpRequestException>() // 所有网络异常都重试
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => CalculateRetryDelay(retryAttempt, retryDelayMs));
    }

    /// <summary>
    /// 判断 HttpResponse 是否属于可重试情形
    /// </summary>
    private static bool IsRetriableResponse(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;

        // 明确可重试的 4xx
        if (statusCode == 408 || statusCode == 429)
            return true;

        // 5xx 服务器错误：可重试
        if (statusCode >= 500 && statusCode < 600)
            return true;

        // 其他 4xx（客户端错误）：不可重试
        if (statusCode >= 400 && statusCode < 500)
            return false;

        // 1xx, 2xx, 3xx：成功或信息响应，不重试
        if (statusCode < 400)
            return false;

        // 未知状态码（如 600+）：保守起见，允许重试
        return true;
    }

    /// <summary>
    /// 计算重试延迟（指数退避 + 抖动）
    /// </summary>
    /// <param name="retryAttempt">当前重试次数（从 1 开始）</param>
    /// <param name="baseDelayMs">基础延迟（毫秒）</param>
    /// <returns>实际延迟时间</returns>
    /// <remarks>
    /// 延迟公式：min(baseDelayMs * 2^(attempt-1) * jitter, 30秒)
    /// 抖动因子范围：0.8 ～ 1.2（±20%），避免惊群效应
    /// </remarks>
    private static TimeSpan CalculateRetryDelay(int retryAttempt, int baseDelayMs)
    {
        // 指数退避
        var exponentialDelay = baseDelayMs * Math.Pow(2, retryAttempt - 1);

        // 抖动（使用静态 Random 实例）
        var jitterFactor = 0.8 + (JitterRandom.NextDouble() * 0.4); // [0.8, 1.2)

        var finalDelayMs = (long)(exponentialDelay * jitterFactor);

        // 限制最大延迟为 30 秒
        const long MaxDelayMs = 30_000;
        finalDelayMs = Math.Min(finalDelayMs, MaxDelayMs);

        return TimeSpan.FromMilliseconds(finalDelayMs);
    }
}