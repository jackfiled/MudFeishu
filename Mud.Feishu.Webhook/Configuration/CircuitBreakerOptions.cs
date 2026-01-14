// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Configuration;


/// <summary>
/// 断路器配置选项
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// 断路器断开前的连续失败次数，默认 5
    /// </summary>
    public int ExceptionsAllowedBeforeBreaking { get; set; } = 5;

    /// <summary>
    /// 断路器保持开启状态的持续时间，默认 30 秒
    /// </summary>
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 断路器进入半开状态后的成功次数阈值，默认 3
    /// 达到此成功次数后，断路器重置为关闭状态
    /// </summary>
    public int SuccessThresholdToReset { get; set; } = 3;
}