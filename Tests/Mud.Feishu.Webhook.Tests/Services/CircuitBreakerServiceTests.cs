// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Services;

/// <summary>
/// CircuitBreakerService 单元测试
/// </summary>
public class CircuitBreakerServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenSuccessful_ShouldReturnResult()
    {
        // Arrange
        var service = new CircuitBreakerService();
        var expectedResult = "success";

        // Act
        var result = await service.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            return expectedResult;
        });

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(CircuitState.Closed, service.State);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFailuresExceedThreshold_ShouldOpenCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            ExceptionsAllowedBeforeBreaking = 3,
            DurationOfBreak = TimeSpan.FromSeconds(1)
        };
        var service = new CircuitBreakerService(options);

        // Act - 触发3次失败
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await service.ExecuteAsync<string>(async () =>
                {
                    await Task.Delay(10);
                    throw new Exception("Test failure");
                });
            }
            catch
            {
                // 预期的异常
            }
        }

        // Assert
        Assert.Equal(CircuitState.Open, service.State);
        Assert.Equal(3, service.FailureCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCircuitOpen_ShouldThrowCircuitBreakerOpenException()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            ExceptionsAllowedBeforeBreaking = 2,
            DurationOfBreak = TimeSpan.FromSeconds(10)
        };
        var service = new CircuitBreakerService(options);

        // 触发失败打开断路器
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await service.ExecuteAsync<string>(async () => throw new Exception("Test"));
            }
            catch { }
        }

        // Act & Assert
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            async () => await service.ExecuteAsync(async () => await Task.CompletedTask));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCircuitHalfOpen_ShouldAllowRequest()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            ExceptionsAllowedBeforeBreaking = 2,
            DurationOfBreak = TimeSpan.FromMilliseconds(100),
            SuccessThresholdToReset = 2
        };
        var service = new CircuitBreakerService(options);

        // 打开断路器
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await service.ExecuteAsync<string>(async () => throw new Exception("Test"));
            }
            catch { }
        }

        // 等待进入半开状态
        await Task.Delay(150);

        // Act
        var result = await service.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            return "success";
        });

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(CircuitState.HalfOpen, service.State);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHalfOpenAndSuccessful_ShouldCloseCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            ExceptionsAllowedBeforeBreaking = 2,
            DurationOfBreak = TimeSpan.FromMilliseconds(100),
            SuccessThresholdToReset = 2
        };
        var service = new CircuitBreakerService(options);

        // 打开断路器
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await service.ExecuteAsync<string>(async () => throw new Exception("Test"));
            }
            catch { }
        }

        // 等待进入半开状态
        await Task.Delay(150);

        // Act - 执行成功操作达到阈值
        for (int i = 0; i < 2; i++)
        {
            await service.ExecuteAsync(async () => await Task.CompletedTask);
        }

        // Assert
        Assert.Equal(CircuitState.Closed, service.State);
        Assert.Equal(0, service.FailureCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHalfOpenAndFails_ShouldReopenCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            ExceptionsAllowedBeforeBreaking = 2,
            DurationOfBreak = TimeSpan.FromMilliseconds(100)
        };
        var service = new CircuitBreakerService(options);

        // 打开断路器
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await service.ExecuteAsync<string>(async () => throw new Exception("Test"));
            }
            catch { }
        }

        // 等待进入半开状态
        await Task.Delay(150);

        // Act - 半开状态下再次失败
        try
        {
            await service.ExecuteAsync<string>(async () => throw new Exception("Test"));
        }
        catch { }

        // Assert
        Assert.Equal(CircuitState.Open, service.State);
    }

    [Fact]
    public async Task Reset_ShouldResetCircuitToClosedState()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            ExceptionsAllowedBeforeBreaking = 1
        };
        var service = new CircuitBreakerService(options);

        // 打开断路器
        try
        {
            await service.ExecuteAsync<string>(async () => throw new Exception("Test"));
        }
        catch { }

        // Act
        service.Reset();

        // Assert
        Assert.Equal(CircuitState.Closed, service.State);
        Assert.Equal(0, service.FailureCount);
    }

    [Fact]
    public void Trip_ShouldManuallyOpenCircuit()
    {
        // Arrange
        var service = new CircuitBreakerService();

        // Act
        service.Trip();

        // Assert
        Assert.Equal(CircuitState.Open, service.State);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutReturnValue_ShouldWork()
    {
        // Arrange
        var service = new CircuitBreakerService();
        var executed = false;

        // Act
        await service.ExecuteAsync(async () =>
        {
            await Task.Delay(10);
            executed = true;
        });

        // Assert
        Assert.True(executed);
        Assert.Equal(CircuitState.Closed, service.State);
    }
}
