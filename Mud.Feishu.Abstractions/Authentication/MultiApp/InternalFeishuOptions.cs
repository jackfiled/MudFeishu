// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书配置选项（简化版，用于TokenManager内部）
/// </summary>
/// <remarks>
/// 这是内部使用的简化版配置类，仅包含TokenManager需要的核心配置信息。
/// 用于在TokenManager与新的多应用配置模型之间进行适配。
/// 
/// 注意：此类标记为internal，仅供内部使用，外部应使用 FeishuAppConfig。
/// </remarks>
internal class InternalFeishuOptions
{
    /// <summary>
    /// 飞书应用ID
    /// </summary>
#if NET7_0_OR_GREATER
        required
#endif
  public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// 飞书应用密钥
    /// </summary>
#if NET7_0_OR_GREATER
        required
#endif
  public string AppSecret { get; set; } = string.Empty;

    /// <summary>
    /// API基础地址
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// 请求超时时间（秒）
    /// </summary>
    public int TimeOut { get; set; } = 30;

    /// <summary>
    /// 失败重试次数
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// 重试延迟时间（毫秒）
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// 令牌刷新阈值（秒）
    /// </summary>
    public int TokenRefreshThreshold { get; set; } = 300;

    /// <summary>
    /// 是否启用日志
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// 从 FeishuAppConfig 创建 InternalFeishuOptions
    /// </summary>
    /// <param name="config">应用配置</param>
    /// <returns>飞书选项</returns>
    public static InternalFeishuOptions FromAppConfig(FeishuAppConfig config)
    {
        return new InternalFeishuOptions
        {
            AppId = config.AppId,
            AppSecret = config.AppSecret,
            BaseUrl = config.BaseUrl,
            TimeOut = config.TimeOut,
            RetryCount = config.RetryCount,
            RetryDelayMs = config.RetryDelayMs,
            TokenRefreshThreshold = config.TokenRefreshThreshold,
            EnableLogging = config.EnableLogging
        };
    }
}
