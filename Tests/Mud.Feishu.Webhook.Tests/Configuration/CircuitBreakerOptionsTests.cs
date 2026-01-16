// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Configuration;

/// <summary>
/// CircuitBreakerOptions 单元测试
/// </summary>
public class CircuitBreakerOptionsTests
{
    [Fact]
    public void CircuitBreakerOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new CircuitBreakerOptions();

        // Assert
        Assert.Equal(5, options.ExceptionsAllowedBeforeBreaking);
        Assert.Equal(TimeSpan.FromSeconds(30), options.DurationOfBreak);
        Assert.Equal(3, options.SuccessThresholdToReset);
    }

    [Fact]
    public void CircuitBreakerOptions_SetCustomValues_ShouldWork()
    {
        // Arrange & Act
        var options = new CircuitBreakerOptions
        {
            ExceptionsAllowedBeforeBreaking = 10,
            DurationOfBreak = TimeSpan.FromMinutes(5),
            SuccessThresholdToReset = 5
        };

        // Assert
        Assert.Equal(10, options.ExceptionsAllowedBeforeBreaking);
        Assert.Equal(TimeSpan.FromMinutes(5), options.DurationOfBreak);
        Assert.Equal(5, options.SuccessThresholdToReset);
    }

    [Fact]
    public void CircuitBreakerOptions_SetExceptionsThreshold_ShouldAcceptPositiveValues()
    {
        // Arrange & Act
        var options1 = new CircuitBreakerOptions { ExceptionsAllowedBeforeBreaking = 1 };
        var options2 = new CircuitBreakerOptions { ExceptionsAllowedBeforeBreaking = 100 };

        // Assert
        Assert.Equal(1, options1.ExceptionsAllowedBeforeBreaking);
        Assert.Equal(100, options2.ExceptionsAllowedBeforeBreaking);
    }

    [Fact]
    public void CircuitBreakerOptions_SetDurationOfBreak_ShouldAcceptDifferentTimeSpans()
    {
        // Arrange & Act
        var options1 = new CircuitBreakerOptions { DurationOfBreak = TimeSpan.FromSeconds(10) };
        var options2 = new CircuitBreakerOptions { DurationOfBreak = TimeSpan.FromMinutes(10) };
        var options3 = new CircuitBreakerOptions { DurationOfBreak = TimeSpan.FromHours(1) };

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(10), options1.DurationOfBreak);
        Assert.Equal(TimeSpan.FromMinutes(10), options2.DurationOfBreak);
        Assert.Equal(TimeSpan.FromHours(1), options3.DurationOfBreak);
    }

    [Fact]
    public void CircuitBreakerOptions_SetSuccessThreshold_ShouldAcceptPositiveValues()
    {
        // Arrange & Act
        var options1 = new CircuitBreakerOptions { SuccessThresholdToReset = 1 };
        var options2 = new CircuitBreakerOptions { SuccessThresholdToReset = 10 };

        // Assert
        Assert.Equal(1, options1.SuccessThresholdToReset);
        Assert.Equal(10, options2.SuccessThresholdToReset);
    }
}
