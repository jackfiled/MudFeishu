// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.TokenManager;

/// <summary>
/// 凭证令牌数据模型
/// </summary>
/// <remarks>
/// 表示从飞书API获取的访问令牌信息，包含令牌内容、过期时间和响应状态。
/// </remarks>
public class CredentialToken
{
    /// <summary>
    /// 响应消息
    /// </summary>
    /// <remarks>
    /// API返回的错误消息或成功消息，null表示无消息。
    /// </remarks>
    public string? Msg { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    /// <remarks>
    /// 0表示成功，非0表示错误状态码。
    /// </remarks>
    public int Code { get; set; }

    /// <summary>
    /// 令牌过期时间戳（毫秒）
    /// </summary>
    /// <remarks>
    /// Unix时间戳格式的过期时间，使用UTC时间。
    /// </remarks>
    public
#if NET7_0_OR_GREATER
    required
#endif
  long Expire
        { get; set; }

    /// <summary>
    /// 访问令牌
    /// </summary>
    /// <remarks>
    /// 用于API认证的访问令牌字符串，null表示未获取到令牌。
    /// </remarks>
    public
#if NET7_0_OR_GREATER
    required
#endif
  string? AccessToken
        { get; set; }
}
