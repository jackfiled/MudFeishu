// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护；使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用；许可证 位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Moq;
using Mud.CodeGenerator;
using Mud.Feishu.Abstractions;
using Mud.Feishu.DataModels;
using Mud.Feishu.TokenManager;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication.TokenManager;

/// <summary>
/// 用户令牌管理器测试（通过 FeishuAppContext 接口测试）
/// </summary>
/// <remarks>
/// 由于 UserTokenManager 现在是 internal 类，测试通过 FeishuAppContext 公开的 IUserTokenManager 接口进行测试。
/// </remarks>
public class UserTokenManagerTests : TokenManagerTestsBase
{
    private readonly IUserTokenManager _userTokenManager;
    private readonly FeishuAppConfig _config;

    public UserTokenManagerTests() : base()
    {
        _userTokenManager = AppContext.UserTokenManager;
        _config = AppContext.Config;
    }

    [Fact]
    public async Task GetUserTokenWithCodeAsync_ShouldReturnNull_WhenApiReturnsNull()
    {
        // Arrange
        _authenticationApiMock
            .Setup(x => x.GetOAuthenAccessTokenAsync(It.IsAny<OAuthTokenRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuthCredentialsResult?)null);

        // Act
        var result = await _userTokenManager.GetUserTokenWithCodeAsync("test-code", "https://example.com/callback", CancellationToken.None);

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
            .ReturnsAsync((OAuthCredentialsResult?)null);

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
    public async Task GetTokenAsync_ShouldThrowArgumentException_WhenUserIdIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _userTokenManager.GetTokenAsync(null, CancellationToken.None));
    }
}
