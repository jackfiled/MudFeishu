// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook.Configuration;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Services;

/// <summary>
/// FeishuEventValidator 单元测试
/// </summary>
public class FeishuEventValidatorTests
{
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventValidator>> _loggerMock;
    private readonly Mock<IFeishuNonceDistributedDeduplicator> _nonceDeduplicatorMock;

    public FeishuEventValidatorTests()
    {
        _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventValidator>>();
        _nonceDeduplicatorMock = new Mock<IFeishuNonceDistributedDeduplicator>();
    }

    [Fact]
    public void ValidateSubscriptionRequest_WhenValidRequest_ShouldReturnTrue()
    {
        // Arrange
        var options = new FeishuWebhookOptions { VerificationToken = "test_token" };
        var validator = new FeishuEventValidator(
            _loggerMock.Object,
            _nonceDeduplicatorMock.Object,
            Options.Create(options),
            null);
        var request = new Mud.Feishu.Webhook.Models.EventVerificationRequest
        {
            Type = "url_verification",
            Token = "test_token",
            Challenge = "test_challenge"
        };

        // Act
        var result = validator.ValidateSubscriptionRequest(request, "test_token");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateSubscriptionRequest_WhenInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var options = new FeishuWebhookOptions { VerificationToken = "correct_token" };
        var validator = new FeishuEventValidator(
            _loggerMock.Object,
            _nonceDeduplicatorMock.Object,
            Options.Create(options),
            null);
        var request = new Mud.Feishu.Webhook.Models.EventVerificationRequest
        {
            Type = "url_verification",
            Token = "wrong_token",
            Challenge = "test_challenge"
        };

        // Act
        var result = validator.ValidateSubscriptionRequest(request, "correct_token");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateSubscriptionRequest_WhenInvalidType_ShouldReturnFalse()
    {
        // Arrange
        var options = new FeishuWebhookOptions { VerificationToken = "test_token" };
        var validator = new FeishuEventValidator(
            _loggerMock.Object,
            _nonceDeduplicatorMock.Object,
            Options.Create(options),
            null);
        var request = new Mud.Feishu.Webhook.Models.EventVerificationRequest
        {
            Type = "invalid_type",
            Token = "test_token",
            Challenge = "test_challenge"
        };

        // Act
        var result = validator.ValidateSubscriptionRequest(request, "test_token");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSignatureAsync_WhenSignatureIsValid_ShouldReturnTrue()
    {
        // Arrange
        var options = new FeishuWebhookOptions
        {
            EncryptKey = "test_encrypt_key",
            TimestampToleranceSeconds = 60
        };
        var validator = new FeishuEventValidator(
            _loggerMock.Object,
            _nonceDeduplicatorMock.Object,
            Options.Create(options),
            null);

        _nonceDeduplicatorMock
            .Setup(x => x.TryMarkAsUsedAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test_nonce";
        var body = "{\"test\":\"data\"}";
        var encrypt = body;

        // 计算签名
        var signString = $"{timestamp}\n{nonce}\n{encrypt}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes("test_encrypt_key"));
        var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signString));
        var signature = System.Convert.ToBase64String(hashBytes);

        // Act
        var result = await validator.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, "test_encrypt_key");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateSignatureAsync_WhenSignatureIsInvalid_ShouldReturnFalse()
    {
        // Arrange
        var options = new FeishuWebhookOptions
        {
            EncryptKey = "test_encrypt_key",
            TimestampToleranceSeconds = 60
        };
        var validator = new FeishuEventValidator(
            _loggerMock.Object,
            _nonceDeduplicatorMock.Object,
            Options.Create(options),
            null);

        _nonceDeduplicatorMock
            .Setup(x => x.TryMarkAsUsedAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = "test_nonce";
        var body = "{\"test\":\"data\"}";
        var encrypt = body;
        var signature = "invalid_signature";

        // Act
        var result = await validator.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, "test_encrypt_key");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateSignatureAsync_WhenTimestampIsTooOld_ShouldReturnFalse()
    {
        // Arrange
        var options = new FeishuWebhookOptions
        {
            EncryptKey = "test_encrypt_key",
            TimestampToleranceSeconds = 60
        };
        var validator = new FeishuEventValidator(
            _loggerMock.Object,
            _nonceDeduplicatorMock.Object,
            Options.Create(options),
            null);

        var timestamp = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds();
        var nonce = "test_nonce";
        var encrypt = "{\"test\":\"data\"}";
        var signature = "signature";

        // Act
        var result = await validator.ValidateSignatureAsync(timestamp, nonce, encrypt, signature, "test_encrypt_key");

        // Assert
        Assert.False(result);
    }
}
