// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mud.CodeGenerator;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Exceptions;
using Mud.Feishu.DataModels;
using Mud.Feishu.TokenManager;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication.TokenManager;

public class AppTokenManagerTests
{
    private readonly Mock<IFeishuAuthentication> _authenticationApiMock;
    private readonly Mock<IOptions<FeishuOptions>> _optionsMock;
    private readonly Mock<ILogger<TokenManagerWithCache>> _loggerMock;
    private readonly Mock<ITokenCache> _tokenCacheMock;
    private readonly FeishuOptions _feishuOptions;
    private readonly AppTokenManager _appTokenManager;

    public AppTokenManagerTests()
    {
        _authenticationApiMock = new Mock<IFeishuAuthentication>();
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
        _tokenCacheMock.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0));

        _appTokenManager = new AppTokenManager(
            _authenticationApiMock.Object,
            _optionsMock.Object,
            _loggerMock.Object,
            _tokenCacheMock.Object);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnBearerToken_WhenApiReturnsValidToken()
    {
        // Arrange
        var expectedToken = "test-app-access-token";
        var tokenExpire = 7200;
        var apiResult = new AppCredentialResult
        {
            AppAccessToken = expectedToken,
            Expire = tokenExpire,
            Code = 0,
            Msg = "ok"
        };

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        // Act
        var result = await _appTokenManager.GetTokenAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal($"Bearer {expectedToken}", result);
        _authenticationApiMock.Verify(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
        _tokenCacheMock.Verify(x => x.SetAsync(It.IsAny<string>(), expectedToken, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnCachedToken_WhenCacheHasValidToken()
    {
        // Arrange
        var cachedToken = "Bearer cached-token";
        _tokenCacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedToken);

        // Act
        var result = await _appTokenManager.GetTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal(cachedToken, result);
        _authenticationApiMock.Verify(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnNull_WhenApiReturnsNull()
    {
        // Arrange
        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppCredentialResult)null);

        // Act & Assert
        await Assert.ThrowsAsync<FeishuException>(() => _appTokenManager.GetTokenAsync(CancellationToken.None));
        _authenticationApiMock.Verify(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrowFeishuException_WhenApiReturnsError()
    {
        // Arrange
        var apiResult = new AppCredentialResult
        {
            Code = 400,
            Msg = "Invalid app credentials"
        };

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FeishuException>(() => _appTokenManager.GetTokenAsync(CancellationToken.None));
        Assert.Equal(400, exception.ErrorCode);
        Assert.Contains("Invalid app credentials", exception.Message);
        _authenticationApiMock.Verify(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReuseCachedToken_WhenTokenIsValid()
    {
        // Arrange
        var expectedToken = "test-app-access-token";
        var tokenExpire = 7200;
        var apiResult = new AppCredentialResult
        {
            AppAccessToken = expectedToken,
            Expire = tokenExpire,
            Code = 0,
            Msg = "ok"
        };

        var callCount = 0;
        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? apiResult : null;
            });

        _tokenCacheMock.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                return callCount == 0 ? (string?)null : $"Bearer {expectedToken}";
            });

        // Act - First call should get new token
        var result1 = await _appTokenManager.GetTokenAsync(CancellationToken.None);

        // Act - Second call should use cached token
        var result2 = await _appTokenManager.GetTokenAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal($"Bearer {expectedToken}", result1);
        Assert.Equal(result1, result2);

        // Verify API was only called once
        _authenticationApiMock.Verify(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanExpiredTokens_ShouldCallCacheCleanExpiredAsync()
    {
        // Act
        _appTokenManager.CleanExpiredTokens();

        // Assert
        _tokenCacheMock.Verify(x => x.CleanExpiredAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCacheStatistics_ShouldCallCacheGetStatisticsAsync()
    {
        // Arrange
        _tokenCacheMock.Setup(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((5, 2));

        // Act
        var stats = await _appTokenManager.GetCacheStatistics();

        // Assert
        Assert.Equal(5, stats.Total);
        Assert.Equal(2, stats.Expired);
        _tokenCacheMock.Verify(x => x.GetStatisticsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
