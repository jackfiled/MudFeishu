# 拦截器示例

本目录包含飞书 Webhook 事件拦截器的实现示例。

## 文件列表

| 文件 | 描述 |
|------|------|
| `AuditLogInterceptor.cs` | 审计日志拦截器 - 记录所有事件处理详情 |
| `PerformanceMonitoringInterceptor.cs` | 性能监控拦截器 - 记录事件处理耗时 |

## 如何使用

在 `Program.cs` 中注册拦截器：

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration, "FeishuWebhook")
    .AddInterceptor<AuditLogInterceptor>()          // 审计日志
    .AddInterceptor<PerformanceMonitoringInterceptor>() // 性能监控
    .AddHandler<...>()
    .Build();
```

## 详细说明

请参考 [README-INTERCEPTORS.md](../README-INTERCEPTORS.md) 获取详细的使用文档。
