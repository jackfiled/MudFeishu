// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using static Mud.Feishu.TokenManager.TokenManagerWithCache;

namespace Mud.Feishu.TokenManager;

/// <summary>
/// 用户令牌管理。
/// </summary>
public interface IUserTokenManager : ITokenManager
{
    /// <summary>
    /// 使用授权码获取用户令牌
    /// </summary>
    /// <param name="code">授权码</param>
    /// <param name="redirectUri">重定向地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户令牌信息</returns>
    Task<CredentialToken?> GetUserTokenWithCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default);


    /// <summary>
    /// 使用刷新令牌获取新的用户令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新的用户令牌信息</returns>
    Task<CredentialToken?> RefreshUserTokenAsync(string refreshToken, CancellationToken cancellationToken = default);


}
