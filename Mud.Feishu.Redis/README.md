# Mud.Feishu.Redis

飞书事件订阅组件 Redis 分布式去重扩展。

## 功能特性

- **事件去重**: 基于 EventId 的分布式去重，防止重复处理同一事件
- **Nonce 去重**: 防止重放攻击，确保请求的唯一性
- **SeqID 去重**: WebSocket 二进制消息序列号去重，防止重复处理
- **原子性操作**: 使用 Redis SETNX + EXPIRE 确保去重操作的原子性
- **自动过期**: Redis 自动清理过期数据，无需手动维护
- **分布式支持**: 适用于多实例部署场景

## 安装

```bash
dotnet add package Mud.Feishu.Redis
```

## 快速开始

### 配置 Redis 连接

在 `appsettings.json` 中添加配置：

```json
{
  "Feishu": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "EventCacheExpiration": "24:00:00",
      "NonceTtl": "00:05:00",
      "SeqIdCacheExpiration": "24:00:00",
      "EventKeyPrefix": "feishu:event:",
      "NonceKeyPrefix": "feishu:nonce:",
      "SeqIdKeyPrefix": "feishu:seqid:",
      "ConnectTimeout": 5000,
      "SyncTimeout": 5000,
      "Ssl": false,
      "AllowAdmin": true
    }
  }
}
```

### 注册服务

#### 注册所有去重服务

```csharp
using Mud.Feishu.Redis.Extensions;

// 自动从配置文件读取 Redis 连接信息并注册所有去重服务
builder.Services
    .AddFeishuRedis()
    .AddFeishuRedisDeduplicators();
```

#### 单独注册服务

```csharp
// 只注册事件去重服务
builder.Services
    .AddFeishuRedis()
    .AddFeishuRedisEventDeduplicator();

// 只注册 Nonce 去重服务
builder.Services
    .AddFeishuRedis()
    .AddFeishuRedisNonceDeduplicator();

// 只注册 SeqID 去重服务
builder.Services
    .AddFeishuRedis()
    .AddFeishuRedisSeqIDDeduplicator();
```

### 完整示例

```csharp
var builder = WebApplication.CreateBuilder(args);

// 注册 Redis 服务（从配置文件读取）
builder.Services
    .AddFeishuRedis()
    .AddFeishuRedisDeduplicators()
    .AddFeishuWebSocket(options =>
    {
        options.AppId = builder.Configuration["Feishu:AppId"];
        options.AppSecret = builder.Configuration["Feishu:AppSecret"];
    });

var app = builder.Build();
app.Run();
```

## 配置选项

### RedisOptions 配置项

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ConnectionString` | string | "localhost:6379" | Redis 连接字符串 |
| `EventCacheExpiration` | TimeSpan | 24小时 | 事件去重缓存过期时间 |
| `NonceTtl` | TimeSpan | 5分钟 | Nonce 有效期 |
| `SeqIdCacheExpiration` | TimeSpan | 24小时 | SeqID 去重缓存过期时间 |
| `EventKeyPrefix` | string | "feishu:event:" | 事件去重键前缀 |
| `NonceKeyPrefix` | string | "feishu:nonce:" | Nonce 去重键前缀 |
| `SeqIdKeyPrefix` | string | "feishu:seqid:" | SeqID 去重键前缀 |
| `ConnectTimeout` | int | 5000ms | 连接超时时间 |
| `SyncTimeout` | int | 5000ms | 同步超时时间 |
| `Ssl` | bool | false | 是否启用 TLS/SSL |
| `AllowAdmin` | bool | true | 是否允许管理员操作 |

## Redis 数据结构

### 事件去重

- **Key**: `{keyPrefix}{eventId}`
- **Type**: String
- **Value**: "1"
- **TTL**: 由 `cacheExpiration` 指定

### Nonce 去重

- **Key**: `{keyPrefix}{nonce}`
- **Type**: String
- **Value**: "1"
- **TTL**: 由 `nonceTtl` 指定

### SeqID 去重

- **Key**: `{keyPrefix}{seqId}`
- **Type**: String
- **Value**: "1"
- **TTL**: 由 `cacheExpiration` 指定
- **Sorted Set**: `{keyPrefix}set` (用于记录已处理的 SeqID)

## 常见问题

### 1. 如何更改默认的缓存过期时间？

在配置文件中修改：

```json
{
  "Feishu": {
    "Redis": {
      "EventCacheExpiration": "7.00:00:00",
      "NonceTtl": "00:10:00",
      "SeqIdCacheExpiration": "7.00:00:00"
    }
  }
}
```

### 2. 多个环境如何隔离数据？

通过配置文件设置不同的键前缀：

开发环境 `appsettings.Development.json`:
```json
{
  "Feishu": {
    "Redis": {
      "EventKeyPrefix": "dev:feishu:event:",
      "NonceKeyPrefix": "dev:feishu:nonce:",
      "SeqIdKeyPrefix": "dev:feishu:seqid:"
    }
  }
}
```

生产环境 `appsettings.Production.json`:
```json
{
  "Feishu": {
    "Redis": {
      "EventKeyPrefix": "prod:feishu:event:",
      "NonceKeyPrefix": "prod:feishu:nonce:",
      "SeqIdKeyPrefix": "prod:feishu:seqid:"
    }
  }
}
```

### 3. 如何监控 Redis 去重状态？

每个去重服务都提供了日志记录，可以通过日志查看去重状态：

```csharp
// 获取已处理的事件数量
var deduplicator = serviceProvider.GetRequiredService<IFeishuEventDistributedDeduplicator>();
var count = await deduplicator.GetCachedCountAsync();
```

### 4. 如何使用 TLS/SSL 连接 Redis？

在配置文件中启用 SSL：

```json
{
  "Feishu": {
    "Redis": {
      "ConnectionString": "secure.redis.com:6380,password=xxx",
      "Ssl": true
    }
  }
}
```

或使用 `rediss://` 协议：

```json
{
  "Feishu": {
    "Redis": {
      "ConnectionString": "rediss://secure.redis.com:6380"
    }
  }
}
```

### 5. Redis 异常时的降级策略

当 Redis 连接失败或操作异常时，本库提供了多种降级策略：

#### 5.1 标准去重器的异常行为

`RedisFeishuEventDistributedDeduplicator`、`RedisFeishuNonceDistributedDeduplicator` 和 `RedisFeishuSeqIDDeduplicator` 在 Redis 异常时的行为：

- **IsDuplicateAsync**: 返回 `false`（允许处理事件，但记录错误日志）
- **MarkAsProcessedAsync**: 返回 `false`（标记失败，但记录错误日志）

**影响**: Redis 故障时可能导致重复处理事件，但不会阻塞整个系统。

**适用场景**: 对重复处理容忍度较高的场景，如日志记录、统计分析等。

#### 5.2 带降级的去重器

`RedisFeishuEventDistributedDeduplicatorWithFallback` 提供了更智能的降级策略：

- Redis 正常时使用 Redis 去重
- Redis 故障时自动降级到内存去重
- 支持 fallback 配置（内存缓存过期时间、清理间隔等）

**使用示例**:

```csharp
builder.Services
    .AddFeishuRedis()
    .AddFeishuRedisEventDeduplicatorWithFallback(options =>
    {
        options.MemoryCacheExpiration = TimeSpan.FromHours(1);
        options.MemoryCacheCleanupInterval = TimeSpan.FromMinutes(10);
    });
```

**适用场景**: 对去重要求较高，但可以在 Redis 故障时暂时降级到内存去重的场景。

#### 5.3 选择建议

| 去重器类型 | Redis 故障时行为 | 适用场景 |
|-----------|----------------|---------|
| `RedisFeishuEventDistributedDeduplicator` | 允许重复处理，不阻塞 | 日志分析、统计等容忍重复的场景 |
| `RedisFeishuEventDistributedDeduplicatorWithFallback` | 降级到内存去重 | 对去重要求高，可接受临时内存存储的场景 |
| 自定义处理 | 抛出异常或自定义逻辑 | 需要精确控制异常行为的场景 |

#### 5.4 最佳实践

1. **监控 Redis 健康状态**: 实现健康检查，及时发现 Redis 故障
2. **配置告警**: 当 Redis 连接失败超过阈值时发送告警
3. **定期备份**: 对于关键业务，定期将内存缓存同步到 Redis
4. **使用连接池**: 配置合理的连接池大小，提高 Redis 连接稳定性
5. **启用 Redis 持久化**: 使用 RDB 或 AOF 持久化，防止数据丢失

```csharp
// 建议配置
builder.Services
    .AddFeishuRedis(options =>
    {
        options.ConnectionString = "localhost:6379";
        options.ConnectTimeout = 5000;
        options.SyncTimeout = 5000;
        options.ConnectRetry = 3;
    })
    .AddFeishuRedisEventDeduplicatorWithFallback(fallbackOptions =>
    {
        fallbackOptions.MemoryCacheExpiration = TimeSpan.FromHours(2);
        fallbackOptions.MemoryCacheCleanupInterval = TimeSpan.FromMinutes(5);
    });

// 添加健康检查
builder.Services.AddHealthChecks()
    .AddRedis(options.ConnectionString, name: "redis");
```

## 许可证

MIT License
