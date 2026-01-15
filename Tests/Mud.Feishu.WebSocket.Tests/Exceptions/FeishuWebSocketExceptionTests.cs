// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.WebSocket.Exceptions;
using Xunit;

namespace Mud.Feishu.WebSocket.Tests.Exceptions;

/// <summary>
/// 自定义异常类单元测试
/// </summary>
public class FeishuWebSocketExceptionTests
{
    [Fact]
    public void FeishuWebSocketException_ShouldSetProperties()
    {
        // Arrange & Act
        var ex = new FeishuWebSocketException("TestError", "Test message", isRecoverable: false);

        // Assert
        Assert.Equal("TestError", ex.ErrorType);
        Assert.Equal("Test message", ex.Message);
        Assert.False(ex.IsRecoverable);
    }

    [Fact]
    public void FeishuConnectionException_ShouldSetProperties()
    {
        // Arrange & Act
        var ex = new FeishuConnectionException("Connection failed", "Connecting");

        // Assert
        Assert.Equal("Connecting", ex.ConnectionState);
        Assert.Equal("ConnectionError", ex.ErrorType);
        Assert.True(ex.IsRecoverable);
    }

    [Fact]
    public void FeishuAuthenticationException_ShouldSetProperties()
    {
        // Arrange & Act
        var ex = new FeishuAuthenticationException("Auth failed", errorCode: 401);

        // Assert
        Assert.Equal(401, ex.ErrorCode);
        Assert.Equal("AuthenticationError", ex.ErrorType);
        Assert.False(ex.IsRecoverable);
    }

    [Fact]
    public void FeishuMessageException_ShouldSetProperties()
    {
        // Arrange & Act
        var ex = new FeishuMessageException("Invalid format", "Json");

        // Assert
        Assert.Equal("Json", ex.MessageType);
        Assert.Equal("MessageError", ex.ErrorType);
        Assert.True(ex.IsRecoverable);
    }

    [Fact]
    public void FeishuNetworkException_ShouldSetProperties()
    {
        // Arrange & Act
        var ex = new FeishuNetworkException("Network error", -2147467259);

        // Assert
        Assert.Equal(-2147467259, ex.NetworkErrorCode);
        Assert.Equal("NetworkError", ex.ErrorType);
        Assert.True(ex.IsRecoverable);
    }
}
