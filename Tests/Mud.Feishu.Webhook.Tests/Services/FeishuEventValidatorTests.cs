// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Moq;
using Mud.Feishu.Webhook.Configuration;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Services;

/// <summary>
/// FeishuEventValidator 单元测试
/// </summary>
public class FeishuEventValidatorTests
{
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventValidator>> _loggerMock;

    public FeishuEventValidatorTests()
    {
        _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventValidator>>();
    }

    [Fact]
    public void ValidateTimestamp_WhenTimestampIsWithinWindow_ShouldReturnTrue()
    {
        // Arrange
        var validator = new FeishuEventValidator(_loggerMock.Object, new FeishuWebhookOptions());
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Act
        var result = validator.ValidateTimestamp(timestamp);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateTimestamp_WhenTimestampIsTooOld_ShouldReturnFalse()
    {
        // Arrange
        var validator = new FeishuEventValidator(_loggerMock.Object, new FeishuWebhookOptions());
        var timestamp = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds();

        // Act
        var result = validator.ValidateTimestamp(timestamp);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ValidateTimestamp_WhenTimestampIsInFuture_ShouldReturnFalse()
    {
        // Arrange
        var validator = new FeishuEventValidator(_loggerMock.Object, new FeishuWebhookOptions());
        var timestamp = DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds();

        // Act
        var result = validator.ValidateTimestamp(timestamp);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ValidateSignature_WhenSignatureIsValid_ShouldReturnTrue()
    {
        // Arrange
        var options = new FeishuWebhookOptions { EncryptKey = "test_encrypt_key" };
        var validator = new FeishuEventValidator(_loggerMock.Object, options);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test_nonce";
        var body = "{\"test\":\"data\"}";
        var signature = validator.ComputeSignature(timestamp, nonce, body);

        // Act
        var result = validator.ValidateSignature(timestamp, nonce, body, signature);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateSignature_WhenSignatureIsInvalid_ShouldReturnFalse()
    {
        // Arrange
        var options = new FeishuWebhookOptions { EncryptKey = "test_encrypt_key" };
        var validator = new FeishuEventValidator(_loggerMock.Object, options);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test_nonce";
        var body = "{\"test\":\"data\"}";
        var signature = "invalid_signature";

        // Act
        var result = validator.ValidateSignature(timestamp, nonce, body, signature);

        // Assert
        Assert.False(result.IsSuccess);
    }
}
