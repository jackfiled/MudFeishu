using FluentAssertions;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Configuration;

/// <summary>
/// FeishuOptions 配置测试
/// </summary>
public class FeishuOptionsTests
{
    private const string ValidAppId = "cli_a1b2c3d4e5f6g7h8i9j0";
    private const string ValidAppSecret = "test-secret-123456789012345";

    [Fact]
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = ValidAppId,
            AppSecret = ValidAppSecret,
            BaseUrl = "https://open.feishu.cn",
            TimeOut = 30,
            RetryCount = 3
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyAppId_ShouldThrow(string appId)
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = appId,
            AppSecret = ValidAppSecret
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("123")]
    [InlineData("test_app")]
    public void Validate_WithInvalidAppIdFormat_ShouldThrow(string appId)
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = appId,
            AppSecret = ValidAppSecret
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        exception.Message.Should().Contain("AppId 格式无效");
    }

    [Fact]
    public void Validate_WithShortAppId_ShouldThrow()
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = "cli_123",
            AppSecret = ValidAppSecret
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        exception.Message.Should().Contain("AppId 长度");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyAppSecret_ShouldThrow(string appSecret)
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = ValidAppId,
            AppSecret = appSecret
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithShortAppSecret_ShouldThrow()
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = ValidAppId,
            AppSecret = "short"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        exception.Message.Should().Contain("AppSecret 长度");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(301)]
    public void Validate_WithInvalidTimeOut_ShouldThrow(int timeOut)
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = ValidAppId,
            AppSecret = ValidAppSecret,
            TimeOut = timeOut
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        exception.Message.Should().Contain("TimeOut");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Validate_WithInvalidRetryCount_ShouldThrow(int retryCount)
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = ValidAppId,
            AppSecret = ValidAppSecret,
            RetryCount = retryCount
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        exception.Message.Should().Contain("RetryCount");
    }

    [Theory]
    [InlineData(30)]
    [InlineData(59)]
    [InlineData(3601)]
    public void Validate_WithInvalidTokenRefreshThreshold_ShouldThrow(int threshold)
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = ValidAppId,
            AppSecret = ValidAppSecret,
            TokenRefreshThreshold = threshold
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        exception.Message.Should().Contain("TokenRefreshThreshold");
    }

    [Theory]
    [InlineData(99)]
    [InlineData(60001)]
    public void Validate_WithInvalidRetryDelayMs_ShouldThrow(int retryDelayMs)
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = ValidAppId,
            AppSecret = ValidAppSecret,
            RetryDelayMs = retryDelayMs
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        exception.Message.Should().Contain("RetryDelayMs");
    }

    [Fact]
    public void RetryDelayMs_WithValidValue_ShouldNotThrow()
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = ValidAppId,
            AppSecret = ValidAppSecret,
            RetryDelayMs = 5000
        };

        // Act & Assert
        var exception = Record.Exception(() => options.Validate());
        exception.Should().BeNull();
    }

    [Theory]
    [InlineData("invalid-url")]
    [InlineData("ftp://invalid.com")]
    [InlineData("javascript:alert(1)")]
    public void Validate_WithInvalidBaseUrl_ShouldThrow(string baseUrl)
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = ValidAppId,
            AppSecret = ValidAppSecret,
            BaseUrl = baseUrl
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => options.Validate());
        exception.Message.Should().Contain("BaseUrl");
    }

    [Fact]
    public void TimeOut_WithValueBelowMinimum_ShouldClampToMinimum()
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = ValidAppId,
            AppSecret = ValidAppSecret
        };

        // Act
        options.TimeOut = -10;

        // Assert
        options.TimeOut.Should().Be(1);
    }

    [Fact]
    public void TimeOut_WithValueAboveMaximum_ShouldClampToMaximum()
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = ValidAppId,
            AppSecret = ValidAppSecret
        };

        // Act
        options.TimeOut = 500;

        // Assert
        options.TimeOut.Should().Be(300);
    }

    [Fact]
    public void RetryCount_WithValueBelowMinimum_ShouldClampToMinimum()
    {
        // Arrange
        var options = new FeishuOptions
        {
            AppId = ValidAppId,
            AppSecret = ValidAppSecret
        };

        // Act
        options.RetryCount = -5;

        // Assert
        options.RetryCount.Should().Be(0);
    }

    [Fact]
    public void RetryCount_WithValueAboveMaximum_ShouldClampToMaximum()
    {
        // Arrange
        var options = new FeishuOptions
        {
            AppId = ValidAppId,
            AppSecret = ValidAppSecret
        };

        // Act
        options.RetryCount = 20;

        // Assert
        options.RetryCount.Should().Be(10);
    }

    [Fact]
    public void ToString_ShouldMaskSensitiveData()
    {
        // Arrange
        var options = new Abstractions.FeishuOptions
        {
            AppId = ValidAppId,
            AppSecret = "my-secret-key-1234567890",
            BaseUrl = "https://open.feishu.cn",
            TimeOut = 30
        };

        // Act
        var result = options.ToString();

        // Assert
        result.Should().Contain(ValidAppId);
        result.Should().Contain("my****90");
        result.Should().NotContain("my-secret-key-1234567890");
    }
}
