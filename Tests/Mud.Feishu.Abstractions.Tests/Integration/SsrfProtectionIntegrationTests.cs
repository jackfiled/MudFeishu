using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Utilities;

namespace Mud.Feishu.Abstractions.Tests.Integration;

/// <summary>
/// SSRF 防护集成测试
/// 测试 URL 验证和 SSRF 防护功能
/// </summary>
public class SsrfProtectionIntegrationTests
{
    #region URL Validation Tests

    [Fact]
    public void ValidateBaseUrl_WithFeishuOfficialDomain_ShouldPass()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = "test_app_key",
            AppId = "test_app_id",
            AppSecret = "test_app_secret",
            BaseUrl = "https://open.feishu.cn",
            AllowCustomBaseUrl = false
        };

        // Act & Assert
        var exception = Record.Exception(() => UrlValidator.ValidateBaseUrl(config.BaseUrl, config.AllowCustomBaseUrl));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateBaseUrl_WithPrivateIp_ShouldThrow()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = "test_app_key",
            AppId = "test_app_id",
            AppSecret = "test_app_secret",
            BaseUrl = "https://192.168.1.1",
            AllowCustomBaseUrl = false
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            UrlValidator.ValidateBaseUrl(config.BaseUrl, config.AllowCustomBaseUrl));
        Assert.Contains("私有 IP", exception.Message);
    }

    [Fact]
    public void ValidateBaseUrl_WithHttpProtocol_ShouldThrow()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = "test_app_key",
            AppId = "test_app_id",
            AppSecret = "test_app_secret",
            BaseUrl = "http://open.feishu.cn",
            AllowCustomBaseUrl = false
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            UrlValidator.ValidateBaseUrl(config.BaseUrl, config.AllowCustomBaseUrl));
        Assert.Contains("HTTPS", exception.Message);
    }

    [Fact]
    public void ValidateBaseUrl_WithCustomDomain_WhenNotAllowed_ShouldThrow()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = "test_app_key",
            AppId = "test_app_id",
            AppSecret = "test_app_secret",
            BaseUrl = "https://api.example.com",
            AllowCustomBaseUrl = false
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            UrlValidator.ValidateBaseUrl(config.BaseUrl, config.AllowCustomBaseUrl));
        Assert.Contains("不在白名单", exception.Message);
    }

    [Fact]
    public void ValidateBaseUrl_WithCustomDomain_WhenAllowed_ShouldPass()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = "test_app_key",
            AppId = "test_app_id",
            AppSecret = "test_app_secret",
            BaseUrl = "https://api.example.com",
            AllowCustomBaseUrl = true
        };

        // Act & Assert
        var exception = Record.Exception(() =>
            UrlValidator.ValidateBaseUrl(config.BaseUrl, config.AllowCustomBaseUrl));
        Assert.Null(exception);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ValidateUrl_WithVariousPrivateIpRanges_ShouldBlockAll()
    {
        // Arrange
        var privateIps = new[]
        {
            // 127.0.0.0/8 (Loopback)
            "https://127.0.0.1",
            "https://127.0.0.254",
            "https://127.255.255.254",

            // 10.0.0.0/8 (Private Class A)
            "https://10.0.0.1",
            "https://10.255.255.254",

            // 172.16.0.0/12 (Private Class B)
            "https://172.16.0.1",
            "https://172.31.255.254",

            // 192.168.0.0/16 (Private Class C)
            "https://192.168.0.1",
            "https://192.168.255.254"
        };

        // Act & Assert
        foreach (var url in privateIps)
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                UrlValidator.ValidateUrl(url, false));
            Assert.Contains("私有 IP", exception.Message);
        }
    }

    [Fact]
    public void ValidateBaseUrl_WithFeishuDomains_ShouldAllPass()
    {
        // Arrange
        var feishuDomains = new[]
        {
            "https://open.feishu.cn",
            "https://open.feishu.cn/",
            "https://open.feishu.cn/open-apis",
            "https://open.larksuite.com",
            "https://open.larksuite.com/",
            "https://open.larksuite.com/open-apis"
        };

        // Act & Assert
        foreach (var url in feishuDomains)
        {
            var exception = Record.Exception(() =>
                UrlValidator.ValidateBaseUrl(url, false));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void ValidateUrl_WithNonHttpsProtocols_ShouldAllThrow()
    {
        // Arrange
        var nonHttpsUrls = new[]
        {
            "http://open.feishu.cn",
            "ftp://open.feishu.cn",
            "ws://open.feishu.cn"
        };

        // Act & Assert
        foreach (var url in nonHttpsUrls)
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                UrlValidator.ValidateUrl(url, false));
            Assert.Contains("HTTPS", exception.Message);
        }
    }

    [Fact]
    public void ValidateUrl_WithMaliciousPatterns_ShouldThrow()
    {
        // Arrange
        var maliciousPatterns = new[]
        {
            "http://169.254.169.254/latest/meta-data/", // AWS Metadata
            "http://metadata.google.internal/computeMetadata/v1/", // GCP Metadata
            "http://100.100.100.200/latest/meta-data/", // Aliyun Metadata
            "http://localhost/admin"
        };

        // Act & Assert
        foreach (var url in maliciousPatterns)
        {
            var exception = Assert.ThrowsAny<Exception>(() =>
                UrlValidator.ValidateUrl(url, false));
            // Should throw either for non-HTTPS or private IP
            Assert.NotNull(exception);
        }
    }

    #endregion

    #region Configuration Migration Tests

    [Fact]
    public void OldConfiguration_WithoutAllowCustomBaseUrl_ShouldUseDefaultFalse()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = "test_app_key",
            AppId = "test_app_id",
            AppSecret = "test_app_secret",
            BaseUrl = "https://api.example.com"
            // AllowCustomBaseUrl not set (default is false)
        };

        // Act & Assert - Should throw because custom domain not allowed
        var exception = Assert.Throws<ArgumentException>(() =>
            UrlValidator.ValidateBaseUrl(config.BaseUrl, config.AllowCustomBaseUrl));
        Assert.Contains("不在白名单", exception.Message);
    }

    [Fact]
    public void MigratedConfiguration_WithAllowCustomBaseUrlSet_ShouldWork()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = "test_app_key",
            AppId = "test_app_id",
            AppSecret = "test_app_secret",
            BaseUrl = "https://api.example.com",
            AllowCustomBaseUrl = true
        };

        // Act & Assert - Should pass because custom domain is allowed
        var exception = Record.Exception(() =>
            UrlValidator.ValidateBaseUrl(config.BaseUrl, config.AllowCustomBaseUrl));
        Assert.Null(exception);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void UrlValidation_ShouldBeFast()
    {
        // Arrange
        var validUrl = "https://open.feishu.cn";
        var iterations = 1000;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            UrlValidator.ValidateUrl(validUrl, false);
        }
        stopwatch.Stop();

        // Assert - Should be very fast (less than 10ms for 1000 validations)
        Assert.True(stopwatch.ElapsedMilliseconds < 10,
            $"URL 验证应该很快，但 {iterations} 次验证耗时 {stopwatch.ElapsedMilliseconds}ms");
    }

    #endregion
}
