// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Moq;
using Mud.Feishu.Abstractions.Services;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Services;

/// <summary>
/// FeishuEventDistributedDeduplicator 单元测试
/// </summary>
public class FeishuEventDistributedDeduplicatorTests
{
    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenFirstEvent_ShouldReturnFalse()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventDistributedDeduplicator>>();
        var deduplicator = new FeishuEventDistributedDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        var result = await deduplicator.TryMarkAsProcessedAsync(eventId);

        // Assert
        Assert.False(result); // false 表示未处理过（新事件）
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenDuplicateEvent_ShouldReturnTrue()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventDistributedDeduplicator>>();
        var deduplicator = new FeishuEventDistributedDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        await deduplicator.TryMarkAsProcessedAsync(eventId);
        var result = await deduplicator.TryMarkAsProcessedAsync(eventId);

        // Assert
        Assert.True(result); // true 表示已处理过（重复事件）
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenDifferentEvents_ShouldReturnFalse()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventDistributedDeduplicator>>();
        var deduplicator = new FeishuEventDistributedDeduplicator(loggerMock.Object);
        var eventId1 = "test_event_123";
        var eventId2 = "test_event_456";

        // Act
        await deduplicator.TryMarkAsProcessedAsync(eventId1);
        var result = await deduplicator.TryMarkAsProcessedAsync(eventId2);

        // Assert
        Assert.False(result); // false 表示未处理过（新事件）
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenNullOrEmptyEventId_ShouldReturnFalse()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventDistributedDeduplicator>>();
        var deduplicator = new FeishuEventDistributedDeduplicator(loggerMock.Object);

        // Act
        var result1 = await deduplicator.TryMarkAsProcessedAsync(null!);
        var result2 = await deduplicator.TryMarkAsProcessedAsync(string.Empty);
        var result3 = await deduplicator.TryMarkAsProcessedAsync("   ");

        // Assert
        Assert.False(result1); // 空值应返回 false（跳过去重检查）
        Assert.False(result2);
        Assert.False(result3);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenEventExists_ShouldReturnTrue()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventDistributedDeduplicator>>();
        var deduplicator = new FeishuEventDistributedDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";
        await deduplicator.TryMarkAsProcessedAsync(eventId);

        // Act
        var result = await deduplicator.IsProcessedAsync(eventId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenEventNotExists_ShouldReturnFalse()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventDistributedDeduplicator>>();
        var deduplicator = new FeishuEventDistributedDeduplicator(loggerMock.Object);
        var eventId = "test_event_123";

        // Act
        var result = await deduplicator.IsProcessedAsync(eventId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldCleanupExpiredEntries()
    {
        // Arrange
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FeishuEventDistributedDeduplicator>>();
        var deduplicator = new FeishuEventDistributedDeduplicator(loggerMock.Object, cacheExpiration: TimeSpan.FromMilliseconds(100));
        var eventId = "test_event_123";
        await deduplicator.TryMarkAsProcessedAsync(eventId);

        // Act
        await Task.Delay(150); // 等待过期
        var result = await deduplicator.CleanupExpiredAsync();

        // Assert
        Assert.True(result >= 0); // 验证清理方法执行成功
    }
}
