// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions;

namespace Mud.Feishu.TokenManager;

/// <summary>
/// 用户令牌管理器
/// <para>
/// 注意：此管理器需要通过 OAuth 授权流程获取用户令牌。
/// 服务器端无法自动获取用户令牌，必须经过用户授权后才能使用。
/// </para>
/// <para>
/// 使用流程：
/// 1. 引导用户到飞书授权页面（通过 <see cref="IFeishuV3AuthenticationApi.GetUserAccessTokenAsync"/>）
/// 2. 用户授权后获取授权码 code
/// 3. 使用授权码调用 <see cref="IFeishuV3AuthenticationApi.GetOAuthenAccessTokenAsync"/> 获取用户令牌
/// 4. 令牌有效期2小时，可使用 refresh_token 刷新
/// </para>
/// </summary>
internal class UserTokenManager : TokenManagerWithCache, IUserTokenManager
{
    public UserTokenManager(
       IFeishuV3AuthenticationApi authenticationApi,
       IOptions<FeishuOptions> options,
       ILogger<TokenManagerWithCache> logger) : base(authenticationApi, options, logger, TokenType.UserAccessToken)
    {

    }

    protected override async Task<CredentialToken?> AcquireNewTokenAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "用户令牌无法自动获取。请先通过 OAuth 授权流程获取用户授权码，" +
            "然后使用授权码调用 GetOAuthenAccessTokenAsync 获取用户令牌。" +
            "参考文档：https://open.feishu.cn/document/server-docs/authentication-management/access-token/obtain-user_token");
    }

    /// <summary>
    /// 使用授权码获取用户令牌
    /// </summary>
    /// <param name="code">授权码</param>
    /// <param name="redirectUri">重定向地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户令牌信息</returns>
    public async Task<CredentialToken?> GetUserTokenWithCodeAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        var credentials = new DataModels.OAuthTokenRequest
        {
            GrantType = "authorization_code",
            ClientId = _options.AppId,
            ClientSecret = _options.AppSecret,
            Code = code,
            RedirectUri = redirectUri
        };

        var res = await _authenticationApi.GetOAuthenAccessTokenAsync(credentials, cancellationToken);
        if (res == null) return null;

        return new CredentialToken
        {
            AccessToken = res.AccessToken ?? string.Empty,
            Expire = res.ExpiresIn,
            Code = res.Code,
            Msg = res.ErrorDescription ?? "Success"
        };
    }

    /// <summary>
    /// 使用刷新令牌获取新的用户令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新的用户令牌信息</returns>
    public async Task<CredentialToken?> RefreshUserTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var credentials = new DataModels.OAuthRefreshTokenRequest
        {
            GrantType = "refresh_token",
            ClientId = _options.AppId,
            ClientSecret = _options.AppSecret,
            RefreshToken = refreshToken
        };

        var res = await _authenticationApi.GetOAuthenRefreshAccessTokenAsync(credentials, cancellationToken);
        if (res == null) return null;

        return new CredentialToken
        {
            AccessToken = res.AccessToken ?? string.Empty,
            Expire = res.ExpiresIn,
            Code = res.Code,
            Msg = res.ErrorDescription ?? "Success"
        };
    }
}

