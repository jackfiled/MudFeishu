# WebSocket Demo 拦截器使用示例

本 Demo 项目展示了如何使用事件拦截器来增强飞书 WebSocket 事件处理功能。

## 已注册的拦截器

在 `Program.cs` 中，我们注册了 3 个拦截器，它们按顺序执行：

```csharp
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddInterceptor<LoggingEventInterceptor>()           // 1. 日志拦截器（内置）
    .AddInterceptor<WebSocketTelemetryInterceptor>()      // 2. 遥测拦截器（自定义）
    .AddInterceptor<RateLimitingInterceptor>(...)        // 3. 限流拦截器（自定义）
    .AddHandler<...>()
    .Build();
```

## 拦截器执行流程

```
WebSocket 接收到事件消息
    ↓
LoggingEventInterceptor (BeforeHandleAsync) → 记录事件开始
    ↓
WebSocketTelemetryInterceptor (BeforeHandleAsync) → 创建 OTel Activity，统计总数
    ↓
RateLimitingInterceptor (BeforeHandleAsync) → 检查频率限制
    ↓
[如果未被限流]
    ↓
[事件处理器处理事件]
    ↓
[如果未被限流]
    ↓
RateLimitingInterceptor (AfterHandleAsync) → 空实现
    ↓
WebSocketTelemetryInterceptor (AfterHandleAsync) → 完成 Activity，统计失败数
    ↓
LoggingEventInterceptor (AfterHandleAsync) → 记录事件结束
```

## 内置拦截器

### 1. LoggingEventInterceptor
**位置**: `Mud.Feishu.Abstractions.Interceptors.LoggingEventInterceptor`

**功能**:
- 在事件处理前后记录基础日志信息
- 记录事件类型、事件 ID、租户密钥等基本信息

**日志示例**:
```
[INFO] 事件开始处理: EventType=department.user.created_v4, EventId=xxx, TenantKey=xxx
[INFO] 事件处理完成: EventType=department.user.created_v4, EventId=xxx
```

## 自定义拦截器

### 2. WebSocketTelemetryInterceptor
**位置**: `Mud.Feishu.WebSocket.Demo.Interceptors.WebSocketTelemetryInterceptor`

**功能**:
- 使用 OpenTelemetry API 收集 WebSocket 事件遥测数据
- 创建 Activity 并设置标签（事件类型、事件 ID、租户密钥）
- 统计总事件数和失败事件数
- 提供统计信息查询方法

**配置**:
```csharp
.AddInterceptor<WebSocketTelemetryInterceptor>()
```

**统计方法**:
```csharp
// 获取统计信息
var (totalEvents, failedEvents, successRate) = telemetryInterceptor.GetStatistics();
Console.WriteLine($"总事件数: {totalEvents}");
Console.WriteLine($"失败事件数: {failedEvents}");
Console.WriteLine($"成功率: {successRate:F2}%");
```

**实现**:
```csharp
public class WebSocketTelemetryInterceptor : IFeishuEventInterceptor
{
    private readonly ActivitySource _activitySource;
    private readonly ConcurrentDictionary<string, Activity> _activities = new();
    private int _totalEvents;
    private int _failedEvents;

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        var activity = _activitySource.StartActivity($"WebSocketEvent_{eventType}");
        if (activity != null)
        {
            activity.SetTag("component", "Mud.Feishu.WebSocket.Demo");
            activity.SetTag("event.type", eventType);
            activity.SetTag("event.id", eventData.EventId);
            activity.SetTag("event.tenant_key", eventData.TenantKey);
        }

        var eventId = eventData.EventId ?? Guid.NewGuid().ToString();
        _activities.TryAdd(eventId, activity);

        Interlocked.Increment(ref _totalEvents);
        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        var eventId = eventData.EventId ?? string.Empty;

        if (_activities.TryRemove(eventId, out var activity))
        {
            if (exception != null)
            {
                Interlocked.Increment(ref _failedEvents);
                activity?.SetTag("error", true);
                activity?.SetTag("error.message", exception.Message);
                activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }

            activity?.Dispose();
        }

        return Task.CompletedTask;
    }

    public (int TotalEvents, int FailedEvents, double SuccessRate) GetStatistics()
    {
        var total = Interlocked.CompareExchange(ref _totalEvents, 0, 0);
        var failed = Interlocked.CompareExchange(ref _failedEvents, 0, 0);
        var successRate = total > 0 ? (double)(total - failed) / total * 100 : 100;
        return (total, failed, successRate);
    }
}
```

### 3. RateLimitingInterceptor
**位置**: `Mud.Feishu.WebSocket.Demo.Interceptors.RateLimitingInterceptor`

**功能**:
- 对同一租户 + 事件类型进行频率限制
- 防止高频事件导致系统过载
- 可配置最小处理间隔

**配置**:
```csharp
.AddInterceptor<RateLimitingInterceptor>(sp => new RateLimitingInterceptor(
    sp.GetRequiredService<ILogger<RateLimitingInterceptor>>(),
    minIntervalMs: 50)) // 50ms 最小间隔
```

**日志示例**:
```
[WARNING] [限流] 事件处理过快，已限流: EventType=department.user.created_v4, EventId=xxx, TenantKey=xxx, ElapsedMs=30, MinIntervalMs=50
```

**实现**:
```csharp
public class RateLimitingInterceptor : IFeishuEventInterceptor
{
    private readonly ConcurrentDictionary<string, DateTime> _lastProcessedTimes = new();
    private readonly TimeSpan _minInterval;

    public RateLimitingInterceptor(ILogger<RateLimitingInterceptor> logger, int minIntervalMs = 100)
    {
        _logger = logger;
        _minInterval = TimeSpan.FromMilliseconds(minIntervalMs);
    }

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        var key = $"{eventData.TenantKey}:{eventType}";

        if (_lastProcessedTimes.TryGetValue(key, out var lastProcessed))
        {
            var elapsed = DateTime.UtcNow - lastProcessed;
            if (elapsed < _minInterval)
            {
                _logger.LogWarning(
                    "[限流] 事件处理过快，已限流: EventType={EventType}, EventId={EventId}, " +
                    "TenantKey={TenantKey}, ElapsedMs={ElapsedMs}, MinIntervalMs={MinIntervalMs}",
                    eventType, eventData.EventId, eventData.TenantKey,
                    elapsed.TotalMilliseconds, _minInterval.TotalMilliseconds);

                return Task.FromResult(false); // 返回 false 中断处理
            }
        }

        _lastProcessedTimes.AddOrUpdate(key, DateTime.UtcNow, (_, _) => DateTime.UtcNow);
        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
```

## 拦截器中断行为

**重要**: 与 Webhook 不同，WebSocket 中的拦截器中断行为：

```
拦截器 1: BeforeHandleAsync 返回 true → 继续下一个拦截器
拦截器 2: BeforeHandleAsync 返回 false → 中断所有后续拦截器和事件处理
拦截器 3: BeforeHandleAsync → 不执行
事件处理器 → 不执行
所有拦截器的 AfterHandleAsync → 仍会执行（按注册顺序）
```

**示例场景**:
```csharp
// 假设有 3 个拦截器，按顺序注册：
// LoggingEventInterceptor → WebSocketTelemetryInterceptor → RateLimitingInterceptor

// 如果 RateLimitingInterceptor 中断了事件处理：
// 1. Logging 和 Telemetry 的 BeforeHandleAsync 已执行
// 2. RateLimiting 的 BeforeHandleAsync 返回 false
// 3. 事件处理器不执行
// 4. 所有 3 个拦截器的 AfterHandleAsync 都会执行
```

## 如何添加自定义拦截器

### 步骤 1: 实现接口

```csharp
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Interceptors;

public class MyWebSocketInterceptor : IFeishuEventInterceptor
{
    private readonly ILogger<MyWebSocketInterceptor> _logger;

    public MyWebSocketInterceptor(ILogger<MyWebSocketInterceptor> logger)
    {
        _logger = logger;
    }

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"[WebSocket拦截器] 收到事件: {eventType}");
        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        if (exception != null)
        {
            _logger.LogError(exception, $"[WebSocket拦截器] 事件处理失败: {eventType}");
        }
        return Task.CompletedTask;
    }
}
```

### 步骤 2: 注册拦截器

```csharp
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddInterceptor<MyWebSocketInterceptor>()  // 注册自定义拦截器
    .AddHandler<...>()
    .Build();
```

### 步骤 3: （可选）使用工厂方法注册

```csharp
.AddInterceptor<MyWebSocketInterceptor>(sp => new MyWebSocketInterceptor(
    sp.GetRequiredService<ILogger<MyWebSocketInterceptor>>(),
    // 其他依赖...
))
```

## WebSocket 特定场景拦截器

### 场景 1: 过滤特定租户的事件

```csharp
public class TenantFilterInterceptor : IFeishuEventInterceptor
{
    private readonly HashSet<string> _blockedTenants;

    public TenantFilterInterceptor(params string[] blockedTenants)
    {
        _blockedTenants = blockedTenants.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        if (_blockedTenants.Contains(eventData.TenantKey))
        {
            return Task.FromResult(false); // 阻止该租户的事件
        }
        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
```

### 场景 2: 统计事件类型分布

```csharp
public class EventTypeStatisticsInterceptor : IFeishuEventInterceptor
{
    private readonly ConcurrentDictionary<string, int> _eventCounts = new();

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        _eventCounts.AddOrUpdate(eventType, 1, (_, count) => count + 1);
        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Dictionary<string, int> GetStatistics()
    {
        return _eventCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
```

### 场景 3: 缓存事件处理结果

```csharp
public class CachedEventResultInterceptor : IFeishuEventInterceptor
{
    private readonly MemoryCache _cache;
    private readonly TimeSpan _cacheDuration;

    public CachedEventResultInterceptor(TimeSpan cacheDuration)
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _cacheDuration = cacheDuration;
    }

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{eventType}:{eventData.EventId}";

        if (_cache.TryGetValue(cacheKey, out var cachedResult))
        {
            // 使用缓存的结果，不继续处理
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        if (exception == null)
        {
            var cacheKey = $"{eventType}:{eventData.EventId}";
            _cache.Set(cacheKey, true, _cacheDuration);
        }

        return Task.CompletedTask;
    }
}
```

## 与 Webhook 拦截器的区别

| 特性 | Webhook 拦截器 | WebSocket 拦截器 |
|------|----------------|------------------|
| **触发时机** | 收到 HTTP POST 请求 | 收到 WebSocket 消息 |
| **中断行为** | 中断后仍执行 AfterHandleAsync | 中断后仍执行 AfterHandleAsync |
| **去重机制** | 支持 Nonce、EventId 去重 | 支持 SeqId 去重 |
| **典型场景** | 安全验证、数据解密 | 频率控制、统计分析 |
| **内置拦截器** | LoggingEventInterceptor、TelemetryEventInterceptor | LoggingEventInterceptor |

## 使用 OpenTelemetry 查看遥测数据

如果配置了 OpenTelemetry，可以在 Jaeger、Zipkin 或其他 OTel 兼容的追踪系统中查看：

1. **安装 OpenTelemetry SDK**:
```bash
dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry.Exporter.Jaeger
dotnet add package OpenTelemetry.Extensions.Hosting
```

2. **配置 OpenTelemetry**:
```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("FeishuWebSocketDemo"))
    .WithTracing(tracing => tracing
        .AddSource("Mud.Feishu.WebSocket.Demo")
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter()
        .AddJaegerExporter());
```

3. **查看追踪数据**: 启动应用并接收 WebSocket 事件后，在 Jaeger UI 中查看完整的追踪链路。

## 注意事项

1. **拦截器顺序很重要**: 按注册顺序执行，先注册的先执行 `BeforeHandleAsync`，后执行 `AfterHandleAsync`
2. **中断处理**: 即使拦截器中断事件处理（返回 false），所有拦截器的 `AfterHandleAsync` 仍会执行
3. **异步处理**: 拦截器方法是异步的，确保正确使用 `await` 和 `CancellationToken`
4. **性能影响**: WebSocket 可能高频推送事件，避免在拦截器中执行耗时操作
5. **线程安全**: WebSocket 环境下多线程并发处理，确保使用线程安全的数据结构
6. **资源清理**: 在 `AfterHandleAsync` 中正确清理资源，避免内存泄漏
7. **状态管理**: 避免在拦截器中维护有状态数据，除非必要（如统计、限流）
