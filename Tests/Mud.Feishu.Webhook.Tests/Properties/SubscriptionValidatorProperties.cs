// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using Mud.Feishu.Webhook.Models;
using Mud.Feishu.Webhook.Services;
using Xunit;

namespace Mud.Feishu.Webhook.Tests.Properties;

/// <summary>
/// 订阅验证器属性测试
/// 使用 FsCheck 进行基于属性的测试，验证订阅验证器的正确性属性
/// </summary>
[Trait("Feature", "feishu-validator-refactoring")]
public class SubscriptionValidatorProperties
{
    private readonly Mock<ILogger<SubscriptionValidator>> _loggerMock;

    public SubscriptionValidatorProperties()
    {
        _loggerMock = new Mock<ILogger<SubscriptionValidator>>();
    }

    /// <summary>
    /// 属性 11: 订阅验证字段检查
    /// **验证需求: 4.2**
    /// 对于任何订阅验证请求，SubscriptionValidator 应该验证请求类型、Token和Challenge字段的完整性
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "11: 订阅验证字段检查")]
    public Property SubscriptionValidator_ShouldValidateAllRequiredFields()
    {
        return Prop.ForAll(
            GenerateSubscriptionRequestData(),
            data =>
            {
                // Arrange
                var validator = new SubscriptionValidator(_loggerMock.Object);
                var request = new EventVerificationRequest
                {
                    Type = data.Type,
                    Token = data.Token,
                    Challenge = data.Challenge
                };

                // Act
                var result = validator.ValidateSubscriptionRequest(request, data.ExpectedToken);

                // Assert
                var hasAllRequiredFields = !string.IsNullOrEmpty(data.Type) &&
                                         !string.IsNullOrEmpty(data.Token) &&
                                         !string.IsNullOrEmpty(data.Challenge);

                var isValidType = data.Type == "url_verification";
                var isTokenMatch = data.Token == data.ExpectedToken;

                var expectedResult = hasAllRequiredFields && isValidType && isTokenMatch;

                return result == expectedResult;
            });
    }

    /// <summary>
    /// 属性 12: 订阅请求类型验证
    /// **验证需求: 4.3**
    /// 对于任何订阅验证请求，只有请求类型为 "url_verification" 的请求应该被接受
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "12: 订阅请求类型验证")]
    public Property SubscriptionValidator_ShouldOnlyAcceptUrlVerificationType()
    {
        return Prop.ForAll(
            GenerateRequestTypeData(),
            data =>
            {
                // Arrange
                var validator = new SubscriptionValidator(_loggerMock.Object);
                var request = new EventVerificationRequest
                {
                    Type = data.Type,
                    Token = "valid-token",
                    Challenge = "valid-challenge"
                };

                // Act
                var result = validator.ValidateSubscriptionRequest(request, "valid-token");

                // Assert
                if (data.Type == "url_verification")
                {
                    return result; // 应该验证通过
                }
                else
                {
                    return !result; // 应该验证失败
                }
            });
    }

    /// <summary>
    /// 属性 13: Token不匹配处理
    /// **验证需求: 4.4**
    /// 对于任何Token不匹配的订阅请求，系统应该拒绝验证并记录警告日志
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "13: Token不匹配处理")]
    public Property SubscriptionValidator_ShouldRejectMismatchedTokens()
    {
        return Prop.ForAll(
            GenerateTokenMismatchData(),
            data =>
            {
                // Arrange
                var loggerMock = new Mock<ILogger<SubscriptionValidator>>();
                var validator = new SubscriptionValidator(loggerMock.Object);
                var request = new EventVerificationRequest
                {
                    Type = "url_verification",
                    Token = data.ActualToken,
                    Challenge = "valid-challenge"
                };

                // Act
                var result = validator.ValidateSubscriptionRequest(request, data.ExpectedToken);

                // Assert
                if (data.ActualToken == data.ExpectedToken && !string.IsNullOrEmpty(data.ActualToken))
                {
                    return result; // Token匹配且不为空，应该验证通过
                }
                else
                {
                    // Token不匹配或为空，应该验证失败
                    var shouldReject = !result;
                    if (shouldReject)
                    {
                        // 根据实际失败原因验证日志
                        if (string.IsNullOrEmpty(data.ActualToken))
                        {
                            VerifyLogCalled(loggerMock, LogLevel.Warning, "验证请求缺少 Token");
                        }
                        else if (data.ActualToken != data.ExpectedToken)
                        {
                            VerifyLogCalled(loggerMock, LogLevel.Warning, "验证 Token 不匹配");
                        }
                    }
                    return shouldReject;
                }
            });
    }

    /// <summary>
    /// 属性 14: 缺失字段处理
    /// **验证需求: 4.5**
    /// 对于任何缺少必要字段的订阅请求，系统应该拒绝验证并记录相应的警告信息
    /// </summary>
    [Property(MaxTest = 100)]
    [Trait("Property", "14: 缺失字段处理")]
    public Property SubscriptionValidator_ShouldRejectMissingFields()
    {
        return Prop.ForAll(
            GenerateMissingFieldData(),
            data =>
            {
                // Arrange
                var loggerMock = new Mock<ILogger<SubscriptionValidator>>();
                var validator = new SubscriptionValidator(loggerMock.Object);

                EventVerificationRequest? request = null;
                if (!data.IsRequestNull)
                {
                    request = new EventVerificationRequest
                    {
                        Type = data.Type ?? "",
                        Token = data.Token ?? "",
                        Challenge = data.Challenge ?? ""
                    };
                }

                // Act
                var result = validator.ValidateSubscriptionRequest(request!, "valid-token");

                // Assert
                var hasMissingFields = data.IsRequestNull ||
                                     string.IsNullOrEmpty(data.Type) ||
                                     string.IsNullOrEmpty(data.Token) ||
                                     string.IsNullOrEmpty(data.Challenge);

                if (hasMissingFields)
                {
                    // 缺少字段，应该验证失败并记录警告日志
                    var shouldReject = !result;
                    if (shouldReject)
                    {
                        if (data.IsRequestNull)
                        {
                            VerifyLogCalled(loggerMock, LogLevel.Warning, "验证请求对象为空");
                        }
                        else if (string.IsNullOrEmpty(data.Type) || data.Type != "url_verification")
                        {
                            VerifyLogCalled(loggerMock, LogLevel.Warning, "无效的验证请求类型");
                        }
                        else if (string.IsNullOrEmpty(data.Token))
                        {
                            VerifyLogCalled(loggerMock, LogLevel.Warning, "验证请求缺少 Token");
                        }
                        else if (string.IsNullOrEmpty(data.Challenge))
                        {
                            VerifyLogCalled(loggerMock, LogLevel.Warning, "验证请求缺少 Challenge");
                        }
                    }
                    return shouldReject;
                }
                else
                {
                    // 没有缺少字段，但可能因为其他原因失败（如类型不匹配）
                    return true; // 属性仍然成立
                }
            });
    }

    /// <summary>
    /// 生成订阅请求数据
    /// </summary>
    private static Arbitrary<SubscriptionRequestData> GenerateSubscriptionRequestData()
    {
        return Arb.From(
            from type in Gen.Elements("url_verification", "invalid_type", "", "event_callback")
            from token in Gen.Elements("valid-token", "invalid-token", "", "another-token")
            from challenge in Gen.Elements("valid-challenge", "test-challenge", "", "another-challenge")
            from expectedToken in Gen.Elements("valid-token", "invalid-token", "another-token")
            select new SubscriptionRequestData
            {
                Type = type,
                Token = token,
                Challenge = challenge,
                ExpectedToken = expectedToken
            });
    }

    /// <summary>
    /// 生成请求类型数据
    /// </summary>
    private static Arbitrary<RequestTypeData> GenerateRequestTypeData()
    {
        return Arb.From(
            from type in Gen.Elements("url_verification", "event_callback", "invalid_type", "", "message", "card_action")
            select new RequestTypeData { Type = type });
    }

    /// <summary>
    /// 生成Token不匹配数据
    /// </summary>
    private static Arbitrary<TokenMismatchData> GenerateTokenMismatchData()
    {
        return Arb.From(
            from actualToken in Gen.Elements("token1", "token2", "valid-token", "invalid-token", "")
            from expectedToken in Gen.Elements("token1", "token2", "valid-token", "different-token")
            select new TokenMismatchData
            {
                ActualToken = actualToken,
                ExpectedToken = expectedToken
            });
    }

    /// <summary>
    /// 生成缺失字段数据
    /// </summary>
    private static Arbitrary<MissingFieldData> GenerateMissingFieldData()
    {
        return Arb.From(
            from isRequestNull in Gen.Elements(true, false)
            from type in Gen.Elements("url_verification", null, "")
            from token in Gen.Elements("valid-token", null, "")
            from challenge in Gen.Elements("valid-challenge", null, "")
            select new MissingFieldData
            {
                IsRequestNull = isRequestNull,
                Type = type,
                Token = token,
                Challenge = challenge
            });
    }

    /// <summary>
    /// 验证日志是否被调用
    /// </summary>
    private static void VerifyLogCalled(Mock<ILogger<SubscriptionValidator>> loggerMock, LogLevel logLevel, string message)
    {
        loggerMock.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// 订阅请求测试数据
    /// </summary>
    public class SubscriptionRequestData
    {
        public string Type { get; set; } = "";
        public string Token { get; set; } = "";
        public string Challenge { get; set; } = "";
        public string ExpectedToken { get; set; } = "";
    }

    /// <summary>
    /// 请求类型测试数据
    /// </summary>
    public class RequestTypeData
    {
        public string Type { get; set; } = "";
    }

    /// <summary>
    /// Token不匹配测试数据
    /// </summary>
    public class TokenMismatchData
    {
        public string ActualToken { get; set; } = "";
        public string ExpectedToken { get; set; } = "";
    }

    /// <summary>
    /// 缺失字段测试数据
    /// </summary>
    public class MissingFieldData
    {
        public bool IsRequestNull { get; set; }
        public string? Type { get; set; }
        public string? Token { get; set; }
        public string? Challenge { get; set; }
    }
}