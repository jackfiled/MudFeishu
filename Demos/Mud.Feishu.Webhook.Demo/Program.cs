// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Interceptors;
using Mud.Feishu.Webhook.Demo;
using Mud.Feishu.Webhook.Demo.Handlers;
using Mud.Feishu.Webhook.Demo.Interceptors;
using Mud.Feishu.Webhook.Demo.Services;

var builder = WebApplication.CreateBuilder(args);

// 注册演示服务
builder.Services.AddSingleton<DemoEventService>();

// 注册飞书Webhook服务（单应用模式）
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration, "FeishuWebhook")
                .AddInterceptor<LoggingEventInterceptor>() // 日志拦截器（内置）
                .AddInterceptor<TelemetryEventInterceptor>(sp => new TelemetryEventInterceptor("Mud.Feishu.Webhook.Demo")) // 遥测拦截器（内置）
                .AddInterceptor<AuditLogInterceptor>() // 审计日志拦截器（自定义）
                .AddInterceptor<PerformanceMonitoringInterceptor>() // 性能监控拦截器（自定义）
                .AddHandler<DemoDepartmentEventHandler>()
                .AddHandler<DemoDepartmentDeleteEventHandler>()
                .AddHandler<DemoDepartmentUpdateEventHandler>()
                .Build();

var app = builder.Build();

// 添加诊断端点
app.MapDiagnostics();

// 添加测试端点（用于捕获飞书回调数据）
app.MapTestEndpoints();

// 添加飞书Webhook中间件（自动注册端点）
app.UseFeishuWebhook();

app.Run();
