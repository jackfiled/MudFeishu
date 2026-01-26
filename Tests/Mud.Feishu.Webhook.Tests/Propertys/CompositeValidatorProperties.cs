// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FsCheck;
using FsCheck.Xunit;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Services;

namespace Mud.Feishu.Webhook.Tests.Propertys;

/// <summary>
/// 组合验证器属性测试
/// 使用 FsCheck 进行基于属性的测试，验证组合验证器的正确性属性
/// </summary>
[Trait("Feature", "feishu-validator-refactoring")]
public class CompositeValidatorProperties
{
    private readonly Mock<ISignatureValidator> _signatureValidatorMock;
    private readonly Mock<ITimestampValidator> _timestampValidatorMock;
    private readonly Mock<INonceValidator> _nonceValidatorMock;
    private readonly Mock<ISubscriptionValidator> _subscriptionValidatorMock;
    private readonly Mock<ILogger<CompositeFeishuEventValidator>> _loggerMock;
    private readonly Mock<IOptions<FeishuWebhookOptions>> _optionsMock;
    private readonly FeishuWebhookOptions _options;

    public CompositeValidatorProperties()
    {
        _signatureValidatorMock = new Mock<ISignatureValidator>();
        _timestampValidatorMock = new Mock<ITimestampValidator>();
        _nonceValidatorMock = new Mock<INonceValidator>();
        _subscriptionValidatorMock = new Mock<ISubscriptionValidator>();
        _loggerMock = new Mock<ILogger<CompositeFeishuEventValidator>>();
        _optionsMock = new Mock<IOptions<FeishuWebhookOptions>>();

        _options = new FeishuWebhookOptions
        {
            TimestampToleranceSeconds = 300,
            EnforceHeaderSignatureValidation = true
        };

        _optionsMock.Setup(x => x.Value).Returns(_options);
    }

    /// <summary>
    /// 属性 15: 组合验证器委托
    /// **验证需求: 5.2, 5.3**
    /// 对于任何 CompositeFeishuEventValidator 的方法调用，应该正确委托给相应的专门验证器
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "15: 组合验证器委托")]
    public Property CompositeValidator_ShouldDelegateToSpecializedValidators()
    {
        return Prop.ForAll(
            GenerateValidationData(),
            data =>
            {
                // Arrange
                var signatureMock = new Mock<ISignatureValidator>();
                var timestampMock = new Mock<ITimestampValidator>();
                var nonceMock = new Mock<INonceValidator>();
                var subscriptionMock = new Mock<ISubscriptionValidator>();

                // 设置Mock返回值
                timestampMock.Setup(x => x.ValidateTimestamp(It.IsAny<long>(), It.IsAny<int>())).Returns(data.TimestampValid);
                nonceMock.Setup(x => x.ValidateNonceAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(data.NonceValid);
                signatureMock.Setup(x => x.ValidateSignatureAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(data.SignatureValid);
                signatureMock.Setup(x => x.ValidateHeaderSignatureAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(data.SignatureValid);
                subscriptionMock.Setup(x => x.ValidateSubscriptionRequest(It.IsAny<EventVerificationRequest>(), It.IsAny<string>())).Returns(data.SubscriptionValid);

                var validator = new CompositeFeishuEventValidator(
                    signatureMock.Object,
                    timestampMock.Object,
                    nonceMock.Object,
                    subscriptionMock.Object,
                    _loggerMock.Object,
                    _optionsMock.Object);

                // Act & Assert - 测试订阅验证委托
                var subscriptionRequest = new EventVerificationRequest { Type = "url_verification", Token = "token", Challenge = "challenge" };
                var subscriptionResult = validator.ValidateSubscriptionRequest(subscriptionRequest, "token");

                // 验证委托调用
                subscriptionMock.Verify(x => x.ValidateSubscriptionRequest(subscriptionRequest, "token"), Times.Once);

                // Act & Assert - 测试时间戳验证委托
                var timestampResult = validator.ValidateTimestamp(data.Timestamp, data.ToleranceSeconds);
                timestampMock.Verify(x => x.ValidateTimestamp(data.Timestamp, data.ToleranceSeconds), Times.Once);

                // 验证结果正确性
                return subscriptionResult == data.SubscriptionValid && timestampResult == data.TimestampValid;
            });
    }

    /// <summary>
    /// 属性 16: 多应用键传播
    /// **验证需求: 5.5**
    /// 对于任何 SetCurrentAppKey 调用，应该正确传播到所有支持多应用的专门验证器
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "16: 多应用键传播")]
    public Property CompositeValidator_ShouldPropagateAppKeyToAllValidators()
    {
        return Prop.ForAll(
            GenerateAppKeyData(),
            appKey =>
            {
                // Arrange
                var signatureMock = new Mock<ISignatureValidator>();
                var timestampMock = new Mock<ITimestampValidator>();
                var nonceMock = new Mock<INonceValidator>();
                var subscriptionMock = new Mock<ISubscriptionValidator>();

                var validator = new CompositeFeishuEventValidator(
                    signatureMock.Object,
                    timestampMock.Object,
                    nonceMock.Object,
                    subscriptionMock.Object,
                    _loggerMock.Object,
                    _optionsMock.Object);

                // Act
                validator.SetCurrentAppKey(appKey);

                // Assert - 验证AppKey被传播到所有支持多应用的验证器
                signatureMock.Verify(x => x.SetCurrentAppKey(appKey), Times.Once);
                nonceMock.Verify(x => x.SetCurrentAppKey(appKey), Times.Once);
                subscriptionMock.Verify(x => x.SetCurrentAppKey(appKey), Times.Once);

                // 时间戳验证器不支持多应用，不应该被调用
                // （因为 ITimestampValidator 没有 SetCurrentAppKey 方法）

                return true;
            });
    }

    /// <summary>
    /// 属性 22: Mock验证器组合
    /// **验证需求: 7.3**
    /// 对于任何使用Mock专门验证器的组合验证器，应该能够正确工作并产生预期的验证结果
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "22: Mock验证器组合")]
    public Property CompositeValidator_ShouldWorkCorrectlyWithMockValidators()
    {
        return Prop.ForAll(
            GenerateSignatureValidationData(),
            data =>
            {
                // Arrange
                var signatureMock = new Mock<ISignatureValidator>();
                var timestampMock = new Mock<ITimestampValidator>();
                var nonceMock = new Mock<INonceValidator>();
                var subscriptionMock = new Mock<ISubscriptionValidator>();

                // 设置Mock行为以模拟不同的验证结果组合
                timestampMock.Setup(x => x.ValidateTimestamp(It.IsAny<long>(), It.IsAny<int>())).Returns(data.TimestampValid);
                nonceMock.Setup(x => x.ValidateNonceAsync(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(data.NonceValid);
                signatureMock.Setup(x => x.ValidateSignatureAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(data.SignatureValid);

                var validator = new CompositeFeishuEventValidator(
                    signatureMock.Object,
                    timestampMock.Object,
                    nonceMock.Object,
                    subscriptionMock.Object,
                    _loggerMock.Object,
                    _optionsMock.Object);

                // Act
                var result = validator.ValidateSignatureAsync(data.Timestamp, data.Nonce, data.Encrypt, data.Signature, data.EncryptKey).Result;

                // Assert - 验证组合逻辑
                // 只有当所有验证都通过时，整体验证才应该通过
                var expectedResult = data.TimestampValid && data.NonceValid && data.SignatureValid;

                // 验证调用顺序和逻辑
                if (data.TimestampValid)
                {
                    // 时间戳验证通过，应该继续验证Nonce
                    nonceMock.Verify(x => x.ValidateNonceAsync(data.Nonce, It.IsAny<bool>()), Times.Once);

                    if (data.NonceValid)
                    {
                        // Nonce验证通过，应该继续验证签名
                        signatureMock.Verify(x => x.ValidateSignatureAsync(data.Timestamp, data.Nonce, data.Encrypt, data.Signature, data.EncryptKey), Times.Once);
                    }
                }

                return result == expectedResult;
            });
    }

    /// <summary>
    /// 生成验证数据
    /// </summary>
    private static Arbitrary<ValidationData> GenerateValidationData()
    {
        return Arb.From(
            from timestamp in Gen.Choose((int)DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds(), (int)DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()).Select(x => (long)x)
            from toleranceSeconds in Gen.Elements(300, 600, 900)
            from timestampValid in Gen.Elements(true, false)
            from nonceValid in Gen.Elements(true, false)
            from signatureValid in Gen.Elements(true, false)
            from subscriptionValid in Gen.Elements(true, false)
            select new ValidationData
            {
                Timestamp = timestamp,
                ToleranceSeconds = toleranceSeconds,
                TimestampValid = timestampValid,
                NonceValid = nonceValid,
                SignatureValid = signatureValid,
                SubscriptionValid = subscriptionValid
            });
    }

    /// <summary>
    /// 生成应用键数据
    /// </summary>
    private static Arbitrary<string> GenerateAppKeyData()
    {
        return Arb.From(
            Gen.Elements("app1", "app2", "test-app", "production-app", "dev-app", ""));
    }

    /// <summary>
    /// 生成签名验证数据
    /// </summary>
    private static Arbitrary<SignatureValidationData> GenerateSignatureValidationData()
    {
        return Arb.From(
            from timestamp in Gen.Choose((int)DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds(), (int)DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()).Select(x => (long)x)
            from nonce in Gen.Elements("nonce1", "nonce2", "test-nonce")
            from encrypt in Gen.Elements("encrypt1", "encrypt2", "test-encrypt")
            from signature in Gen.Elements("sig1", "sig2", "test-signature")
            from encryptKey in Gen.Elements("key1", "key2", "test-key")
            from timestampValid in Gen.Elements(true, false)
            from nonceValid in Gen.Elements(true, false)
            from signatureValid in Gen.Elements(true, false)
            select new SignatureValidationData
            {
                Timestamp = timestamp,
                Nonce = nonce,
                Encrypt = encrypt,
                Signature = signature,
                EncryptKey = encryptKey,
                TimestampValid = timestampValid,
                NonceValid = nonceValid,
                SignatureValid = signatureValid
            });
    }

    /// <summary>
    /// 验证数据
    /// </summary>
    public class ValidationData
    {
        public long Timestamp { get; set; }
        public int ToleranceSeconds { get; set; }
        public bool TimestampValid { get; set; }
        public bool NonceValid { get; set; }
        public bool SignatureValid { get; set; }
        public bool SubscriptionValid { get; set; }
    }

    /// <summary>
    /// 签名验证数据
    /// </summary>
    public class SignatureValidationData
    {
        public long Timestamp { get; set; }
        public string Nonce { get; set; } = "";
        public string Encrypt { get; set; } = "";
        public string Signature { get; set; } = "";
        public string EncryptKey { get; set; } = "";
        public bool TimestampValid { get; set; }
        public bool NonceValid { get; set; }
        public bool SignatureValid { get; set; }
    }
}