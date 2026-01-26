// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 飞书事件时间戳验证器实现
/// 支持秒级和毫秒级时间戳的自动识别和验证
/// </summary>
public class TimestampValidator : ITimestampValidator
{
    private readonly ILogger<TimestampValidator> _logger;

    /// <summary>
    /// 初始化时间戳验证器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public TimestampValidator(ILogger<TimestampValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool ValidateTimestamp(long timestamp, int toleranceSeconds = 300)
    {
        try
        {
            // 如果时间戳为 0，跳过验证（飞书某些请求类型可能不包含时间戳）
            if (timestamp == 0)
            {
                _logger.LogDebug("时间戳为 0，跳过时间戳验证");
                return true;
            }

            // 判断时间戳是秒级还是毫秒级
            // 飞书 X-Lark-Request-Timestamp 请求头使用秒级时间戳（10位）
            // 飞书事件数据中的 create_time 使用毫秒级时间戳（13位）
            DateTimeOffset requestTime;
            if (timestamp < 10000000000) // 小于 100 亿，认为是秒级时间戳
            {
                requestTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
                _logger.LogDebug("识别为秒级时间戳: {Timestamp} -> {RequestTime}", timestamp, requestTime);
            }
            else // 大于等于 100 亿，认为是毫秒级时间戳
            {
                requestTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                _logger.LogDebug("识别为毫秒级时间戳: {Timestamp} -> {RequestTime}", timestamp, requestTime);
            }

            var now = DateTimeOffset.UtcNow;
            var diff = Math.Abs((now - requestTime).TotalSeconds);

            var isValid = diff <= toleranceSeconds;

            if (!isValid)
            {
                _logger.LogWarning("时间戳超出容错范围: 请求时间 {RequestTime}, 当前时间 {CurrentTime}, 差异 {Diff}秒, 容错范围 {Tolerance}秒",
                    requestTime, now, diff, toleranceSeconds);
            }
            else
            {
                _logger.LogDebug("时间戳验证通过: 请求时间 {RequestTime}, 当前时间 {CurrentTime}, 差异 {Diff}秒, 容错范围 {Tolerance}秒",
                    requestTime, now, diff, toleranceSeconds);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证时间戳时发生错误, Timestamp: {Timestamp}", timestamp);
            return false;
        }
    }
}