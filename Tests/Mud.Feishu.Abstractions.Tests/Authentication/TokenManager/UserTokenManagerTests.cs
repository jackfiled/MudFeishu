// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.DataModels;
using Mud.Feishu.TokenManager;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication.TokenManager;

public class UserTokenManagerTests
{
    private readonly Mock<IFeishuV3AuthenticationApi> _authenticationApiMock;
    private readonly Mock<IOptions<FeishuOptions>> _optionsMock;
    private readonly Mock<ILogger<TokenManagerWithCache>> _loggerMock;
    private readonly Mock<ITokenCache> _tokenCacheMock;
    private readonly FeishuOptions _feishuOptions;
    private readonly UserTokenManager _userTokenManager;

    public UserTokenManagerTests()
    {
        _authenticationApiMock = new Mock<IFeishuV3AuthenticationApi>();
        _optionsMock = new Mock<IOptions<FeishuOptions>>();
        _loggerMock = new Mock<ILogger<TokenManagerWithCache>>();
        _tokenCacheMock = new Mock<ITokenCache>();

        _feishuOptions = new FeishuOptions
        {
            AppId = "test-app-id",
            AppSecret = "test-app-secret",
            TokenRefreshThreshold = 300
        };

        _optionsMock.Setup(x => x.Value).Returns(_feishuOptions);
        _tokenCacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        _tokenCacheMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userTokenManager = new UserTokenManager(
            _authenticationApiMock.Object,
            _optionsMock.Object,
            _loggerMock.Object,
            _tokenCacheMock.Object);
    }

    [Fact]
    public async Task GetUserTokenWithCodeAsync_ShouldReturnToken_WhenApiReturnsValidToken()
    {
        // Arrange
        var userId = "user123";
        var authorizationCode = "test-authorization-code";
        var redirectUri = "https://test.example.com/callback";
        var expectedToken = "test-user-access-token";
        var tokenExpire = 7200;

        var apiResult = new OAuthCredentialsResult
        {
            AccessToken = expectedToken,
            ExpiresIn = tokenExpire,
            Code = 0,
            RefreshToken = "test-refresh-token"
        };

        _authenticationApiMock
            .Setup(x => x.GetOAuthenAccessTokenAsync(It.IsAny<OAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        // Act
        var result = await _userTokenManager.GetUserTokenWithCodeAsync(userId, authorizationCode, redirectUri, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedToken, result.AccessToken);
        Assert.Equal(tokenExpire, result.Expire);
        Assert.Equal(0, result.Code);

        _tokenCacheMock.Verify(x => x.SetAsync(
            It.Is<string>(key => key.Contains(userId) && key.Contains(TokenType.UserAccessToken.ToString())),
            expectedToken,
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _authenticationApiMock.Verify(x => x.GetOAuthenAccessTokenAsync(It.Is<OAuthTokenRequest>(req =>
            req.ClientId == _feishuOptions.AppId &&
            req.ClientSecret == _feishuOptions.AppSecret &&
            req.Code == authorizationCode &&
            req.RedirectUri == redirectUri &&
            req.GrantType == "authorization_code"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshUserTokenAsync_ShouldReturnNewToken_WhenApiReturnsValidToken()
    {
        // Arrange
        var userId = "user123";
        var refreshToken = "test-refresh-token";
        var expectedToken = "new-user-access-token";
        var tokenExpire = 7200;

        var apiResult = new OAuthCredentialsResult
        {
            AccessToken = expectedToken,
            ExpiresIn = tokenExpire,
            Code = 0,
            RefreshToken = "new-refresh-token"
        };

        _authenticationApiMock
            .Setup(x => x.GetOAuthenRefreshAccessTokenAsync(It.IsAny<OAuthRefreshTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        // Act
        var result = await _userTokenManager.RefreshUserTokenAsync(userId, refreshToken, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedToken, result.AccessToken);
        Assert.Equal(tokenExpire, result.Expire);
        Assert.Equal(0, result.Code);

        _tokenCacheMock.Verify(x => x.SetAsync(
            It.Is<string>(key => key.Contains(userId) && key.Contains(TokenType.UserAccessToken.ToString())),
            expectedToken,
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _authenticationApiMock.Verify(x => x.GetOAuthenRefreshAccessTokenAsync(It.Is<OAuthRefreshTokenRequest>(req =>
            req.ClientId == _feishuOptions.AppId &&
            req.ClientSecret == _feishuOptions.AppSecret &&
            req.RefreshToken == refreshToken &&
            req.GrantType == "refresh_token"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserTokenWithCodeAsync_ShouldUseDefaultUserId_WhenUserIdIsNull()
    {
        // Arrange
        var authorizationCode = "test-authorization-code";
        var redirectUri = "https://test.example.com/callback";

        var apiResult = new OAuthCredentialsResult
        {
            AccessToken = "test-token",
            ExpiresIn = 7200,
            Code = 0,
            RefreshToken = "test-refresh-token"
        };

        _authenticationApiMock
            .Setup(x => x.GetOAuthenAccessTokenAsync(It.IsAny<OAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        // Act
        await _userTokenManager.GetUserTokenWithCodeAsync(null, authorizationCode, redirectUri, CancellationToken.None);

        // Assert
        _tokenCacheMock.Verify(x => x.SetAsync(
            It.Is<string>(key => key.Contains("default") && key.Contains(TokenType.UserAccessToken.ToString())),
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowNotSupportedException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(() => _userTokenManager.GetTokenAsync(CancellationToken.None));
        Assert.Contains("用户令牌无法自动获取", exception.Message);
    }

    [Fact]
    public async Task GetUserTokenWithCodeAsync_ShouldReturnNull_WhenApiReturnsNull()
    {
        // Arrange
        _authenticationApiMock
            .Setup(x => x.GetOAuthenAccessTokenAsync(It.IsAny<OAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthCredentialsResult)null);

        // Act
        var result = await _userTokenManager.GetUserTokenWithCodeAsync("user123", "test-code", "https://example.com/callback", CancellationToken.None);

        // Assert
        Assert.Null(result);
        _tokenCacheMock.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshUserTokenAsync_ShouldReturnNull_WhenApiReturnsNull()
    {
        // Arrange
        _authenticationApiMock
            .Setup(x => x.GetOAuthenRefreshAccessTokenAsync(It.IsAny<OAuthRefreshTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthCredentialsResult)null);

        // Act
        var result = await _userTokenManager.RefreshUserTokenAsync("user123", "test-refresh-token", CancellationToken.None);

        // Assert
        Assert.Null(result);
        _tokenCacheMock.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DifferentUserTokens_ShouldBeCachedSeparately()
    {
        // Arrange
        var user1Token = "user1-token";
        var user2Token = "user2-token";

        var apiResult1 = new OAuthCredentialsResult
        {
            AccessToken = user1Token,
            ExpiresIn = 7200,
            Code = 0,
            RefreshToken = "refresh1"
        };

        var apiResult2 = new OAuthCredentialsResult
        {
            AccessToken = user2Token,
            ExpiresIn = 7200,
            Code = 0,
            RefreshToken = "refresh2"
        };

        _authenticationApiMock
            .Setup(x => x.GetOAuthenAccessTokenAsync(It.IsAny<OAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthTokenRequest req) =>
                req.Code.Contains("user1") ? apiResult1 : apiResult2);

        // Act
        await _userTokenManager.GetUserTokenWithCodeAsync("user1", "code1", "https://example.com/callback", CancellationToken.None);
        await _userTokenManager.GetUserTokenWithCodeAsync("user2", "code2", "https://example.com/callback", CancellationToken.None);

        // Assert
        _tokenCacheMock.Verify(x => x.SetAsync(
            It.Is<string>(key => key.Contains("user1")),
            user1Token,
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _tokenCacheMock.Verify(x => x.SetAsync(
            It.Is<string>(key => key.Contains("user2")),
            user2Token,
            It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
