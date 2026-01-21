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
/// 1. 引导用户到飞书授权页面（通过 <see cref="IFeishuAuthentication.GetUserAccessTokenAsync"/>）
/// 2. 用户授权后获取授权码 code
/// 3. 使用授权码调用 <see cref="GetUserTokenWithCodeAsync"/> 获取用户令牌
/// 4. 令牌有效期2小时，可使用 refresh_token 刷新
/// </para>
/// </summary>
/// <remarks>
/// 支持多用户令牌缓存，每个用户的令牌独立存储和管理。
/// </remarks>
internal class UserTokenManager(
   IFeishuAuthentication authenticationApi,
   IOptions<FeishuOptions> options,
   ILogger<TokenManagerWithCache> logger,
   ITokenCache tokenCache) : TokenManagerWithCache(authenticationApi, options, logger, tokenCache, TokenType.UserAccessToken), IUserTokenManager
{
    private string? _currentUserId;

    public override async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return await GetTokenAsync(_currentUserId, cancellationToken);
    }

    /// <summary>
    /// 获取用户访问令牌
    /// </summary>
    public async Task<string?> GetTokenAsync(string? userId, CancellationToken cancellationToken = default)
    {
        _currentUserId = userId;
        try
        {
            return await GetTokenInternalAsync(userId, cancellationToken);
        }
        finally
        {
            _currentUserId = null;
        }
    }

    /// <summary>
    /// 内部令牌获取方法（支持多用户缓存）
    /// </summary>
    private async Task<string?> GetTokenInternalAsync(string? userId, CancellationToken cancellationToken)
    {
        // 验证userId不为空
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("UserId cannot be null or empty for user token operations.", nameof(userId));
        }

        var cacheKey = GenerateCacheKey(userId!);

        // 尝试从缓存获取有效令牌
        var cachedToken = await _tokenCache.GetAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedToken))
        {
            _logger.LogDebug("Using cached token for user {UserId}, AppId: {AppId}", userId, _options.AppId);
            return FormatBearerToken(cachedToken);
        }

        // 用户令牌必须通过OAuth授权获取，不支持自动获取
        _logger.LogWarning("No cached token found for user {UserId}, AppId: {AppId}. Please use GetUserTokenWithCodeAsync to obtain a token.", userId, _options.AppId);
        return null;
    }


    protected override async Task<CredentialToken?> AcquireNewTokenAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException(
            "用户令牌无法自动获取。请先通过 OAuth 授权流程获取用户授权码，" +
            "然后使用授权码调用 GetUserTokenWithCodeAsync 获取用户令牌。" +
            "参考文档：https://open.feishu.cn/document/server-docs/authentication-management/access-token/obtain-user_token");
    }


    protected override async Task UpdateTokenCacheAsync(CredentialToken newToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserId))
            throw new InvalidOperationException("Cannot update user token cache without a valid userId.");

        var cacheKey = GenerateCacheKey(_currentUserId);
        var expiresIn = CalculateExpirationFromTimestamp(newToken.Expire);
        await _tokenCache.SetAsync(cacheKey, newToken.AccessToken ?? string.Empty, expiresIn, cancellationToken);
    }


    /// <summary>
    /// 使用授权码获取用户令牌
    /// </summary>
    public async Task<CredentialToken?> GetUserTokenWithCodeAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
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

        var token = new CredentialToken
        {
            AccessToken = res.AccessToken ?? string.Empty,
            Expire = res.ExpiresIn,
            Code = res.Code,
            Msg = res.ErrorDescription ?? "Success"
        };
        var newToken = CreateAppCredentialToken(token);

        var user = await _authenticationApi.GetUserInfoAsync(FormatBearerToken(token.AccessToken), cancellationToken);

        var userId = user?.Data?.UserId;
        // 验证userId不为空
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("UserId cannot be null or empty for user token operations.", nameof(userId));
        }

        _currentUserId = userId;
        // 使用带用户ID的缓存键
        var cacheKey = GenerateCacheKey(userId);
        var expiresIn = CalculateExpirationFromTimestamp(newToken.Expire);
        await _tokenCache.SetAsync(cacheKey, newToken.AccessToken ?? string.Empty, expiresIn, cancellationToken);

        _logger.LogInformation("User token acquired for user {UserId}, AppId: {AppId}, expires at {ExpireTime}",
            userId, _options.AppId, DateTimeOffset.FromUnixTimeMilliseconds(newToken.Expire));

        return token;
    }

    /// <summary>
    /// 使用刷新令牌获取新的用户令牌
    /// </summary>
    public async Task<CredentialToken?> RefreshUserTokenAsync(
        string userId,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        // 验证userId不为空
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("UserId cannot be null or empty for user token operations.", nameof(userId));
        }

        var credentials = new DataModels.OAuthRefreshTokenRequest
        {
            GrantType = "refresh_token",
            ClientId = _options.AppId,
            ClientSecret = _options.AppSecret,
            RefreshToken = refreshToken
        };

        var res = await _authenticationApi.GetOAuthenRefreshAccessTokenAsync(credentials, cancellationToken);
        if (res == null) return null;

        var token = new CredentialToken
        {
            AccessToken = res.AccessToken ?? string.Empty,
            Expire = res.ExpiresIn,
            Code = res.Code,
            Msg = res.ErrorDescription ?? "Success"
        };
        var newToken = CreateAppCredentialToken(token);

        // 使用带用户ID的缓存键
        var cacheKey = GenerateCacheKey(userId);
        var expiresIn = CalculateExpirationFromTimestamp(newToken.Expire);
        await _tokenCache.SetAsync(cacheKey, newToken.AccessToken ?? string.Empty, expiresIn, cancellationToken);

        _logger.LogInformation("User token refreshed for user {UserId}, AppId: {AppId}, expires at {ExpireTime}",
            userId, _options.AppId, DateTimeOffset.FromUnixTimeMilliseconds(newToken.Expire));

        return token;
    }

}
