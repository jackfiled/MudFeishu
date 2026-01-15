# 拦截器示例

本目录包含飞书 WebSocket 事件拦截器的实现示例。

## 文件列表

| 文件 | 描述 |
|------|------|
| `WebSocketTelemetryInterceptor.cs` | 遥测拦截器 - 使用 OpenTelemetry 收集事件指标 |
| `RateLimitingInterceptor.cs` | 限流拦截器 - 对高频事件进行频率限制 |

## 如何使用

在 `Program.cs` 中注册拦截器：

```csharp
builder.Services.CreateFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddInterceptor<WebSocketTelemetryInterceptor>()  // 遥测
    .AddInterceptor<RateLimitingInterceptor>(sp =>    // 限流
        new RateLimitingInterceptor(
            sp.GetRequiredService<ILogger<RateLimitingInterceptor>>(),
            minIntervalMs: 50))
    .AddHandler<...>()
    .Build();
```

## 详细说明

请参考 [README-INTERCEPTORS.md](../README-INTERCEPTORS.md) 获取详细的使用文档。
