using Mud.Feishu.Abstractions.Utilities;

namespace Mud.Feishu.Abstractions.Tests.Utilities;

/// <summary>
/// UrlValidator 单元测试
/// </summary>
public class UrlValidatorTests
{
    #region ValidateBaseUrl Tests

    [Fact]
    public void ValidateBaseUrl_WithOfficialFeishuDomain_ShouldPass()
    {
        // Arrange
        var validUrls = new[]
        {
            "https://open.feishu.cn",
            "https://open.feishu.cn/",
            "https://open.feishu.cn/open-apis",
            "https://open.larksuite.com",
            "https://open.larksuite.com/",
            "https://open.larksuite.com/open-apis"
        };

        // Act & Assert
        foreach (var url in validUrls)
        {
            var exception = Record.Exception(() => UrlValidator.ValidateBaseUrl(url, false));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void ValidateBaseUrl_WithNonHttpsProtocol_ShouldThrow()
    {
        // Arrange
        var invalidUrls = new[]
        {
            "http://open.feishu.cn",
            "ftp://open.feishu.cn",
            "ws://open.feishu.cn"
        };

        // Act & Assert
        foreach (var url in invalidUrls)
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                UrlValidator.ValidateBaseUrl(url, false));
            Assert.Contains("HTTPS", exception.Message);
        }
    }

    [Fact]
    public void ValidateBaseUrl_WithNullUrl_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            UrlValidator.ValidateBaseUrl(null!, false));
    }

    [Fact]
    public void ValidateBaseUrl_WithEmptyUrl_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            UrlValidator.ValidateBaseUrl("", false));
    }

    #endregion

    #region ValidateUrl Tests

    [Fact]
    public void ValidateUrl_WithOfficialFeishuUrl_ShouldPass()
    {
        // Arrange
        var validUrls = new[]
        {
            "https://open.feishu.cn/open-apis/auth/v3/app_access_token/internal",
            "https://open.feishu.cn/open-apis/bot/v3/info"
        };

        // Act & Assert
        foreach (var url in validUrls)
        {
            var exception = Record.Exception(() =>
                UrlValidator.ValidateUrl(url, false));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void ValidateUrl_WithNonHttps_ShouldThrow()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            UrlValidator.ValidateUrl("http://open.feishu.cn/api", false));
        Assert.Contains("HTTPS", exception.Message);
    }

    [Fact]
    public void ValidateUrl_WithNullUrl_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            UrlValidator.ValidateUrl(null!, false));
    }

    [Fact]
    public void ValidateUrl_WithEmptyUrl_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            UrlValidator.ValidateUrl("", false));
    }

    [Fact]
    public void ValidateUrl_WithRelativePath_ShouldThrow()
    {
        // Act & Assert - ValidateUrl only validates absolute URLs
        Assert.Throws<ArgumentException>(() =>
            UrlValidator.ValidateUrl("/api", false));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ValidateUrl_WithFullFeishuApiUrl_ShouldPass()
    {
        // Arrange
        var fullApiUrl = "https://open.feishu.cn/open-apis/auth/v3/app_access_token/internal?app_id=xxx&app_secret=yyy";

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateUrl(fullApiUrl, false));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateBaseUrl_WithFeishuDomainAndPort_ShouldPass()
    {
        // Arrange -飞书域名 + 端口
        var url = "https://open.feishu.cn:443";

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateBaseUrl(url, false));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateBaseUrl_WithFeishuDomainAndPath_ShouldPass()
    {
        // Arrange
        var url = "https://open.feishu.cn/open-apis";

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateBaseUrl(url, false));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateUrl_WithTrailingSlash_ShouldPass()
    {
        // Arrange
        var url = "https://open.feishu.cn/open-apis/";

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateUrl(url, false));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateUrl_WithQueryParams_ShouldPass()
    {
        // Arrange
        var url = "https://open.feishu.cn/open-apis/auth/v3/app_access_token/internal?app_id=test";

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateUrl(url, false));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateUrl_WithFragment_ShouldPass()
    {
        // Arrange
        var url = "https://open.feishu.cn/open-apis/#section";

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateUrl(url, false));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateUrl_WithEncodedCharacters_ShouldPass()
    {
        // Arrange
        var url = "https://open.feishu.cn/open-apis?param=value%20with%20spaces";

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateUrl(url, false));
        Assert.Null(exception);
    }

    #endregion
}
