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
    private readonly Mock<IFeishuV3AuthenticationApi> _authenticationApiMock;
    private readonly Mock<IOptions<FeishuOptions>> _optionsMock;
    private readonly Mock<ILogger<TokenManagerWithCache>> _loggerMock;
    private readonly FeishuOptions _feishuOptions;
    private readonly AppTokenManager _appTokenManager;

    public AppTokenManagerTests()
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

        _appTokenManager = new AppTokenManager(
            _authenticationApiMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);
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

        _authenticationApiMock
            .Setup(x => x.GetAppAccessTokenAsync(It.IsAny<AppCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiResult);

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
    public void CleanExpiredTokens_ShouldRemoveExpiredTokens()
    {
        // Arrange
        var expiredToken = new TokenManagerWithCache.CredentialToken
        {
            AccessToken = "expired-token",
            Expire = DateTimeOffset.UtcNow.AddSeconds(-10).ToUnixTimeMilliseconds(),
            Code = 0,
            Msg = "ok"
        };

        var validToken = new TokenManagerWithCache.CredentialToken
        {
            AccessToken = "valid-token",
            Expire = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeMilliseconds(),
            Code = 0,
            Msg = "ok"
        };

        // Use reflection to access private cache
        var cacheField = typeof(TokenManagerWithCache).GetField("_appTokenCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cache = cacheField?.GetValue(_appTokenManager) as System.Collections.Concurrent.ConcurrentDictionary<string, TokenManagerWithCache.CredentialToken>;

        if (cache != null)
        {
            var cacheKey = $"{_feishuOptions.AppId}:{TokenType.AppAccessToken}";
            cache.TryAdd(cacheKey + ":expired", expiredToken);
            cache.TryAdd(cacheKey + ":valid", validToken);
        }

        // Act
        _appTokenManager.CleanExpiredTokens();

        // Assert - Using statistics to verify
        var stats = _appTokenManager.GetCacheStatistics();
        Assert.Equal(1, stats.Total); // Only valid token should remain
    }
}
