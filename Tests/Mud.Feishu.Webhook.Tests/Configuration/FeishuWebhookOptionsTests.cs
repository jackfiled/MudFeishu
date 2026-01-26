// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Configuration;

/// <summary>
/// FeishuWebhookOptions 单元测试
/// </summary>
public class FeishuWebhookOptionsTests
{
    [Fact]
    public void FeishuWebhookOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new FeishuWebhookOptions();

        // Assert
        Assert.Equal(string.Empty, options.VerificationToken);
        Assert.Equal(string.Empty, options.EncryptKey);
        Assert.True(options.EnableBodySignatureValidation);
        Assert.Equal(30, options.TimestampToleranceSeconds);
        Assert.Equal(30000, options.EventHandlingTimeoutMs);
        Assert.Equal(10, options.MaxConcurrentEvents);
        Assert.True(options.EnableExceptionHandling);
    }

    [Fact]
    public void FeishuWebhookOptions_SetCustomValues_ShouldWork()
    {
        // Arrange & Act
        var options = new FeishuWebhookOptions
        {
            VerificationToken = "custom_token",
            EncryptKey = "custom_key",
            EnableBodySignatureValidation = false,
            TimestampToleranceSeconds = 600,
            EventHandlingTimeoutMs = 10000,
            MaxConcurrentEvents = 200,
            EnableExceptionHandling = false
        };

        // Assert
        Assert.Equal("custom_token", options.VerificationToken);
        Assert.Equal("custom_key", options.EncryptKey);
        Assert.False(options.EnableBodySignatureValidation);
        Assert.Equal(600, options.TimestampToleranceSeconds);
        Assert.Equal(10000, options.EventHandlingTimeoutMs);
        Assert.Equal(200, options.MaxConcurrentEvents);
        Assert.False(options.EnableExceptionHandling);
    }

    [Fact]
    public void FeishuWebhookOptions_SetVerificationToken_ShouldAcceptDifferentFormats()
    {
        // Arrange & Act
        var options1 = new FeishuWebhookOptions { VerificationToken = "simple_token" };
        var options2 = new FeishuWebhookOptions { VerificationToken = "token_with_123_numbers" };
        var options3 = new FeishuWebhookOptions { VerificationToken = "token-with-dashes" };

        // Assert
        Assert.Equal("simple_token", options1.VerificationToken);
        Assert.Equal("token_with_123_numbers", options2.VerificationToken);
        Assert.Equal("token-with-dashes", options3.VerificationToken);
    }

    [Fact]
    public void FeishuWebhookOptions_SetEncryptKey_ShouldAcceptDifferentLengths()
    {
        // Arrange & Act
        var options1 = new FeishuWebhookOptions { EncryptKey = "short_key" };
        var options2 = new FeishuWebhookOptions { EncryptKey = "very_long_encryption_key_with_many_characters_123456" };

        // Assert
        Assert.Equal("short_key", options1.EncryptKey);
        Assert.Equal("very_long_encryption_key_with_many_characters_123456", options2.EncryptKey);
    }

    [Fact]
    public void FeishuWebhookOptions_SetTimeouts_ShouldAcceptValidValues()
    {
        // Arrange & Act
        var options = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = 120,
            EventHandlingTimeoutMs = 30000
        };

        // Assert
        Assert.Equal(120, options.TimestampToleranceSeconds);
        Assert.Equal(30000, options.EventHandlingTimeoutMs);
    }

    [Fact]
    public void FeishuWebhookOptions_SetMaxConcurrentEvents_ShouldAcceptPositiveValues()
    {
        // Arrange & Act
        var options1 = new FeishuWebhookOptions { MaxConcurrentEvents = 1 };
        var options2 = new FeishuWebhookOptions { MaxConcurrentEvents = 500 };
        var options3 = new FeishuWebhookOptions { MaxConcurrentEvents = 1000 };

        // Assert
        Assert.Equal(1, options1.MaxConcurrentEvents);
        Assert.Equal(500, options2.MaxConcurrentEvents);
        Assert.Equal(1000, options3.MaxConcurrentEvents);
    }
}
