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
using Mud.Feishu.Exceptions;
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
    private readonly FeishuOptions _feishuOptions;
    private readonly UserTokenManager _userTokenManager;

    public UserTokenManagerTests()
    {
        _authenticationApiMock = new Mock<IFeishuV3AuthenticationApi>();
        _optionsMock = new Mock<IOptions<FeishuOptions>>();
        _loggerMock = new Mock<ILogger<TokenManagerWithCache>>();

        _feishuOptions = new FeishuOptions
        {
            AppId = "test-app-id",
            AppSecret = "test-app-secret",
            TokenRefreshThreshold = 300
        };

        _optionsMock.Setup(x => x.Value).Returns(_feishuOptions);

        _userTokenManager = new UserTokenManager(
            _authenticationApiMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetUserTokenWithCodeAsync_ShouldReturnToken_WhenApiReturnsValidToken()
    {
        // Arrange
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
        var result = await _userTokenManager.GetUserTokenWithCodeAsync(authorizationCode, redirectUri, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedToken, result.AccessToken);
        Assert.Equal(tokenExpire, result.Expire);
        Assert.Equal(0, result.Code);

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
        var result = await _userTokenManager.RefreshUserTokenAsync(refreshToken, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedToken, result.AccessToken);
        Assert.Equal(tokenExpire, result.Expire);
        Assert.Equal(0, result.Code);

        _authenticationApiMock.Verify(x => x.GetOAuthenRefreshAccessTokenAsync(It.Is<OAuthRefreshTokenRequest>(req =>
            req.ClientId == _feishuOptions.AppId &&
            req.ClientSecret == _feishuOptions.AppSecret &&
            req.RefreshToken == refreshToken &&
            req.GrantType == "refresh_token"),
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
        var result = await _userTokenManager.GetUserTokenWithCodeAsync("test-code", "https://example.com/callback", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RefreshUserTokenAsync_ShouldReturnNull_WhenApiReturnsNull()
    {
        // Arrange
        _authenticationApiMock
            .Setup(x => x.GetOAuthenRefreshAccessTokenAsync(It.IsAny<OAuthRefreshTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthCredentialsResult)null);

        // Act
        var result = await _userTokenManager.RefreshUserTokenAsync("test-refresh-token", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
