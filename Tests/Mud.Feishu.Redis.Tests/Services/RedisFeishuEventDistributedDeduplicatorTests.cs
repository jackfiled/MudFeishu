// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Moq;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Redis.Services;
using StackExchange.Redis;
using Xunit;

namespace Mud.Feishu.Redis.Tests.Services;

/// <summary>
/// RedisFeishuEventDistributedDeduplicator 单元测试
/// </summary>
public class RedisFeishuEventDistributedDeduplicatorTests
{
    private readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<RedisFeishuEventDistributedDeduplicator>> _loggerMock;

    public RedisFeishuEventDistributedDeduplicatorTests()
    {
        _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<RedisFeishuEventDistributedDeduplicator>>();

        _connectionMultiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_databaseMock.Object);
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenFirstEvent_ShouldReturnFalse()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsProcessedAsync("test_event_123");

        // Assert
        Assert.False(result); // false 表示未处理过（新事件）
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenDuplicateEvent_ShouldReturnTrue()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsProcessedAsync("test_event_123");

        // Assert
        Assert.True(result); // true 表示已处理过（重复事件）
    }

    [Fact]
    public async Task TryMarkAsProcessedAsync_WhenRedisFails_ShouldReturnFalse()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Redis connection failed"));

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.TryMarkAsProcessedAsync("test_event_123");

        // Assert
        Assert.False(result); // 异常时返回 false（允许处理）
    }

    [Fact]
    public async Task IsProcessedAsync_WhenEventExists_ShouldReturnTrue()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync("1");

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.IsProcessedAsync("test_event_123");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsProcessedAsync_WhenEventNotExists_ShouldReturnFalse()
    {
        // Arrange
        _databaseMock
            .Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.IsProcessedAsync("test_event_123");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CleanupExpiredAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var deduplicator = new RedisFeishuEventDistributedDeduplicator(
            _connectionMultiplexerMock.Object,
            _loggerMock.Object);

        // Act
        var result = await deduplicator.CleanupExpiredAsync();

        // Assert
        Assert.True(result >= 0); // Redis 实现依赖自动过期，返回值表示状态
    }
}
