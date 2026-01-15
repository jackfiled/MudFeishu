# Webhook Demo 拦截器使用示例

本 Demo 项目展示了如何使用事件拦截器来增强飞书 Webhook 事件处理功能。

## 已注册的拦截器

在 `Program.cs` 中，我们注册了 4 个拦截器，它们按顺序执行：

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration, "FeishuWebhook")
    .AddInterceptor<LoggingEventInterceptor>()           // 1. 日志拦截器（内置）
    .AddInterceptor<TelemetryEventInterceptor>(...)       // 2. 遥测拦截器（内置）
    .AddInterceptor<AuditLogInterceptor>()                 // 3. 审计日志拦截器（自定义）
    .AddInterceptor<PerformanceMonitoringInterceptor>()     // 4. 性能监控拦截器（自定义）
    .AddHandler<...>()
    .Build();
```

## 拦截器执行流程

```
收到 Webhook 请求
    ↓
LoggingEventInterceptor (BeforeHandleAsync) → 记录事件开始
    ↓
TelemetryEventInterceptor (BeforeHandleAsync) → 创建 OpenTelemetry Activity
    ↓
AuditLogInterceptor (BeforeHandleAsync) → 记录审计信息
    ↓
PerformanceMonitoringInterceptor (BeforeHandleAsync) → 启动计时
    ↓
[事件处理器处理事件]
    ↓
PerformanceMonitoringInterceptor (AfterHandleAsync) → 记录耗时
    ↓
AuditLogInterceptor (AfterHandleAsync) → 记录处理结果
    ↓
TelemetryEventInterceptor (AfterHandleAsync) → 完成 Activity
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

### 2. TelemetryEventInterceptor
**位置**: `Mud.Feishu.Abstractions.Interceptors.TelemetryEventInterceptor`

**功能**:
- 使用 OpenTelemetry API 收集遥测数据
- 创建 Activity 并设置标签（事件类型、事件 ID、租户密钥）
- 记录处理状态（成功/失败）和错误信息

**配置**:
```csharp
.AddInterceptor<TelemetryEventInterceptor>(sp => 
    new TelemetryEventInterceptor("Mud.Feishu.Webhook.Demo"))
```

## 自定义拦截器

### 3. AuditLogInterceptor
**位置**: `Mud.Feishu.Webhook.Demo.Interceptors.AuditLogInterceptor`

**功能**:
- 记录详细的审计日志
- 记录事件创建时间、应用 ID 等扩展信息
- 区分成功和失败的处理结果

**日志示例**:
```
[INFO] [审计日志] 事件开始处理: EventType=department.user.created_v4, EventId=xxx, TenantKey=xxx, AppId=cli_xxx, CreateTime=2025-01-14 10:30:00
[INFO] [审计日志] 事件处理成功: EventType=department.user.created_v4, EventId=xxx
```

**实现**:
```csharp
public class AuditLogInterceptor : IFeishuEventInterceptor
{
    private readonly ILogger<AuditLogInterceptor> _logger;

    public AuditLogInterceptor(ILogger<AuditLogInterceptor> logger)
    {
        _logger = logger;
    }

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[审计日志] 事件开始处理: ...");
        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        var status = exception == null ? "成功" : "失败";
        _logger.LogInformation("[审计日志] 事件处理{Status}: ...", status);
        return Task.CompletedTask;
    }
}
```

### 4. PerformanceMonitoringInterceptor
**位置**: `Mud.Feishu.Webhook.Demo.Interceptors.PerformanceMonitoringInterceptor`

**功能**:
- 使用 `Stopwatch` 精确记录事件处理耗时
- 根据耗时输出不同级别的日志（超过 100ms 警告，否则信息）
- 区分成功和失败的处理

**日志示例**:
```
[INFO] [性能监控] 事件处理完成: EventType=department.user.created_v4, EventId=xxx, ElapsedMs=45
[WARNING] [性能监控] 事件处理完成: EventType=department.user.created_v4, EventId=xxx, ElapsedMs=1234
```

**实现**:
```csharp
public class PerformanceMonitoringInterceptor : IFeishuEventInterceptor
{
    private readonly ConcurrentDictionary<string, Stopwatch> _stopwatches = new();

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        var eventId = eventData.EventId ?? Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();
        _stopwatches.TryAdd(eventId, stopwatch);
        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        var eventId = eventData.EventId ?? Guid.NewGuid().ToString();
        if (_stopwatches.TryRemove(eventId, out var stopwatch))
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var logLevel = elapsedMs > 1000 ? LogLevel.Warning : LogLevel.Information;
            _logger.Log(logLevel, "[性能监控] 事件处理完成: ..., ElapsedMs={ElapsedMs}", elapsedMs);
        }
        return Task.CompletedTask;
    }
}
```

## 如何添加自定义拦截器

### 步骤 1: 实现接口

```csharp
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Interceptors;

public class MyCustomInterceptor : IFeishuEventInterceptor
{
    private readonly ILogger<MyCustomInterceptor> _logger;

    public MyCustomInterceptor(ILogger<MyCustomInterceptor> logger)
    {
        _logger = logger;
    }

    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        // 返回 true 继续处理，返回 false 中断处理
        _logger.LogInformation($"自定义拦截器: {eventType}");
        return Task.FromResult(true);
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        // 清理资源或记录处理结果
        return Task.CompletedTask;
    }
}
```

### 步骤 2: 注册拦截器

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration, "FeishuWebhook")
    .AddInterceptor<MyCustomInterceptor>()  // 注册自定义拦截器
    .AddHandler<...>()
    .Build();
```

### 步骤 3: （可选）使用工厂方法注册

```csharp
.AddInterceptor<MyCustomInterceptor>(sp => new MyCustomInterceptor(
    sp.GetRequiredService<ILogger<MyCustomInterceptor>>(),
    // 其他依赖...
))
```

## 拦截器中断示例

如果需要在某些情况下中断事件处理：

```csharp
public class ConditionalInterceptor : IFeishuEventInterceptor
{
    public Task<bool> BeforeHandleAsync(string eventType, EventData eventData, CancellationToken cancellationToken = default)
    {
        // 条件判断
        if (eventType == "sensitive_event" && !IsAuthorized(eventData.TenantKey))
        {
            return Task.FromResult(false); // 中断处理
        }
        return Task.FromResult(true); // 继续处理
    }

    public Task AfterHandleAsync(string eventType, EventData eventData, Exception? exception, CancellationToken cancellationToken = default)
    {
        // 即使中断，AfterHandleAsync 也会执行
        return Task.CompletedTask;
    }
}
```

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
    .ConfigureResource(resource => resource.AddService("FeishuWebhookDemo"))
    .WithTracing(tracing => tracing
        .AddSource("Mud.Feishu.Webhook.Demo")
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter()
        .AddJaegerExporter());
```

3. **查看追踪数据**: 启动应用并接收 Webhook 事件后，在 Jaeger UI 中查看完整的追踪链路。

## 注意事项

1. **拦截器顺序很重要**: 按注册顺序执行，先注册的先执行 `BeforeHandleAsync`，后执行 `AfterHandleAsync`
2. **异步处理**: 拦截器方法是异步的，确保正确使用 `await` 和 `CancellationToken`
3. **性能影响**: 避免在拦截器中执行耗时操作
4. **异常处理**: 拦截器中的异常会影响整个处理流程，需要妥善处理
5. **线程安全**: 如果使用共享状态，确保线程安全（如使用 `ConcurrentDictionary`）
6. **资源清理**: 在 `AfterHandleAsync` 中正确清理资源，避免内存泄漏
