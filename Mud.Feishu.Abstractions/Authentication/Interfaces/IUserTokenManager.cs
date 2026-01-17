// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.TokenManager;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 用户令牌管理接口
/// </summary>
/// <remarks>
/// 提供用户访问令牌（User Access Token）的获取和管理功能。
/// 用户令牌通过OAuth授权流程获取，需要用户授权。
/// </remarks>
public interface IUserTokenManager : ITokenManager
{
    /// <summary>
    /// 获取用户访问令牌
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Bearer格式的用户访问令牌字符串，如果获取失败则返回null</returns>
    /// <remarks>
    /// 此方法会自动处理令牌缓存和刷新逻辑，优先使用缓存中的有效令牌。
    /// 如果缓存中没有有效令牌，则需要先通过OAuth授权流程获取令牌。
    /// 用户令牌的有效期为2小时，会自动在过期前刷新。
    /// </remarks>
    Task<string?> GetTokenAsync(string? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用授权码获取用户令牌
    /// </summary>
    /// <param name="code">授权码</param>
    /// <param name="redirectUri">重定向地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户令牌信息</returns>
    /// <remarks>
    /// 通过OAuth授权码换取用户访问令牌。
    /// 需要先引导用户到飞书授权页面，用户授权后会获得授权码。
    /// 获取到的令牌会自动缓存到对应的用户键下。
    /// </remarks>
    Task<CredentialToken?> GetUserTokenWithCodeAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用刷新令牌获取新的用户令牌
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新的用户令牌信息</returns>
    /// <remarks>
    /// 使用刷新令牌刷新用户访问令牌，无需用户重新授权。
    /// 刷新后的令牌会自动更新到缓存中。
    /// </remarks>
    Task<CredentialToken?> RefreshUserTokenAsync(
        string userId,
        string refreshToken,
        CancellationToken cancellationToken = default);
}
