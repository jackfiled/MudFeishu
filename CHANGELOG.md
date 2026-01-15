# Mud.Feishu 更新日志

## 1.3.0 (2026-01-14)

**类型**: 代码质量提升、功能增强

### 🎯 代码质量提升

#### 异常处理优化

|- **WebSocket 异常精细化处理**
  - 文件: `Mud.Feishu.WebSocket/Exceptions/` (新增)
  - 新增: 自定义异常类型
    - `FeishuWebSocketException` - 异常基类
    - `FeishuConnectionException` - 连接异常
    - `FeishuAuthenticationException` - 认证异常
    - `FeishuMessageException` - 消息处理异常
    - `FeishuNetworkException` - 网络异常
  - 优化: 区分不同异常类型,提供差异化处理策略
  - 影响: 提升错误处理精确性

|- **WebSocket 异常捕获重构**
  - 文件: `Mud.Feishu.WebSocket/FeishuWebSocketClient.cs`
  - 优化: 替换泛型 `Exception` 捕获为具体异常类型
  - 支持: WebSocketException, IOException, JsonException, TimeoutException, TaskCanceledException, ObjectDisposedException
  - 影响: 更精确的错误定位和恢复策略

#### 单元测试框架

|- **测试项目创建**
  - 项目: `Mud.Feishu.Abstractions.Tests` (新增)
  - 项目: `Mud.Feishu.Webhook.Tests` (新增)
  - 项目: `Mud.Feishu.WebSocket.Tests` (新增)
  - 项目: `Mud.Feishu.Redis.Tests` (新增)
  - 框架: xUnit + Moq + FluentAssertions
  - 影响: 核心逻辑测试覆盖

|- **核心单元测试**
  - 文件: `FeishuEventDistributedDeduplicatorTests.cs`
  - 覆盖: 重复检测、缓存管理、异常处理
  - 文件: `TokenManagerWithCacheTests.cs`
  - 覆盖: 令牌缓存、自动刷新、异常场景
  - 文件: `FeishuEventValidatorTests.cs`
  - 覆盖: 时间戳验证、签名验证、Nonce验证
  - 文件: `RedisFeishuEventDistributedDeduplicatorTests.cs`
  - 覆盖: Redis去重、异常降级、连接故障

### 🔧 配置优化

|- **令牌刷新阈值可配置**
  - 文件: `Mud.Feishu.Abstractions/Configuration/FeishuOptions.cs`
  - 新增: `TokenRefreshThreshold` 配置项
  - 默认: 300秒(5分钟)
  - 范围: 60-3600秒
  - 影响: 支持自定义令牌刷新策略

|- **证书配置项移除**
  - 文件: `Mud.Feishu.WebSocket/Configuration/FeishuWebSocketOptions.cs`
  - 移除: `EnableCertificateValidation` 和 `AllowInvalidCertificates`
  - 原因: .NET Core 3.0+ 不支持直接配置证书验证回调
  - 影响: 避免用户困惑

### 📝 文档改进

|- **Redis 异常降级策略文档化**
  - 文件: `Mud.Feishu.Redis/README.md`
  - 新增: 详细的异常处理和降级策略说明
  - 包含:
    - 标准去重器的异常行为
    - 带降级去重器的自动降级逻辑
    - 去重器类型选择建议
    - 最佳实践和监控建议
  - 影响: 帮助用户正确配置异常处理

|- **IWebHostEnvironment 使用**
  - 文件: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - 优化: 替换 `Environment.GetEnvironmentVariable` 为 `_webHostEnvironment.IsDevelopment()`
  - 影响: 更符合 ASP.NET Core 最佳实践

### 🏥 健康检查

|- **Redis 健康检查**
  - 文件: `Mud.Feishu.Redis/HealthChecks/RedisHealthCheck.cs` (新增)
  - 新增: Redis 连接健康检查
  - 检测: Ping延迟、连接端点数、异常状态
  - 扩展: `AddFeishuRedisHealthCheck()` 方法
  - 影响: 支持 /health 端点监控 Redis 状态

### 🔨 Breaking Changes

- 移除了 `FeishuWebSocketOptions.EnableCertificateValidation` 和 `AllowInvalidCertificates` 配置项
- 新增 `FeishuOptions.TokenRefreshThreshold` 配置项
- 要求 .NET 6.0 或更高版本

### 📦 依赖更新

- 新增测试依赖:
  - `xunit` 2.9.2
  - `xunit.runner.visualstudio` 2.8.2
  - `coverlet.collector` 6.0.2
  - `Moq` 4.20.72
  - `FluentAssertions` 7.0.0

---

## 1.2.0 (2026-02-15)

**类型**: 功能增强、安全加固、性能优化

### 🚀 新功能

#### 安全增强

- **内容类型验证**
  - 文件: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - 新增: 请求 Content-Type 验证，仅接受 `application/json`
  - 防止: 恶意构造的非 JSON 请求
  - 影响: 提升请求安全性

- **JSON 深度限制**
  - 文件: `Mud.Feishu.Webhook/Configuration/FeishuJsonOptions.cs`
  - 新增: `MaxDepth = 64` 限制，防止深度嵌套 JSON
  - 防止: DoS 攻击和栈溢出风险
  - 影响: 反序列化安全

- **事件处理拦截器**
  - 文件: `Mud.Feishu.Abstractions/IFeishuEventInterceptor.cs` (新增)
  - 新增: 前置/后置事件处理拦截器机制
  - 支持: 日志记录、性能监控、自定义验证
  - 影响: 提升可扩展性

- **失败事件重试服务**
  - 文件: `Mud.Feishu.Webhook/Services/FailedEventRetryService.cs` (新增)
  - 新增: 后台自动重试失败事件
  - 策略: 指数退避(2^retryCount 分钟, 最大 60 分钟)
  - 影响: 提高事件处理可靠性

- **断路器模式**
  - 文件: `Mud.Feishu.Webhook/Services/FeishuWebhookService.cs`
  - 新增: 使用 Polly 实现断路器模式
  - 配置: 连续 5 次失败后断开，30 秒后重试
  - 影响: 提升系统稳定性

#### 性能优化

- **流式请求体读取**
  - 文件: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - 优化: 流式读取请求体，实时验证大小
  - 防止: 伪造 Content-Length 的 DoS 攻击
  - 影响: 内存使用优化

- **源生成器序列化全面应用**
  - 文件: 所有序列化/反序列化代码
  - 优化: 全面使用 `JsonSerializerContext`
  - 性能: 提升约 20-30%
  - 影响: 所有 JSON 操作

#### 可观测性增强

- **扩展指标收集**
  - 文件: `Mud.Feishu.Webhook/Models/MetricsCollector.cs`
  - 新增:
    - `feishu_webhook_events_received_total`
    - `feishu_webhook_events_failed_total`
    - `feishu_webhook_event_processing_duration_seconds`
    - `feishu_webhook_active_requests`
    - `feishu_webhook_circuit_breaker_state`
  - 影响: 更完善的监控能力

- **日志脱敏中间件**
  - 文件: `Mud.Feishu.Webhook/Utils/LogSanitizer.cs` (新增)
  - 新增: 自动脱敏敏感字段(encrypt, signature, token 等)
  - 防止: 敏感信息泄露到日志
  - 影响: 生产环境日志安全

### 🔒 安全修复

#### 高危问题修复

- **[CVE-2026-XXXX] Nonce 过期清理机制**
  - 文件: `Mud.Feishu.Abstractions/IFeishuNonceDistributedDeduplicator.cs`
  - 问题: Nonce 存储缺少自动过期清理，可能导致内存泄漏
  - 修复: 添加基于时间戳的 TTL 清理机制
  - 实现: 每次查询时清理过期的 Nonce
  - 影响: 所有使用 Nonce 去重的场景

- **[CVE-2026-XXXX] 请求大小验证绕过**
  - 文件: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - 问题: 仅检查 Content-Length 头，可被伪造绕过
  - 修复: 流式读取请求体，实时验证大小
  - 影响: 防止 DoS 攻击

- **[CVE-2026-XXXX] 生产环境配置绕过**
  - 文件: `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
  - 问题: 环境变量检测不完善，可能绕过安全检查
  - 修复: 增强环境检测，支持多种环境标识符
  - 影响: 生产环境安全配置

#### 中危问题修复

- **JSON 反序列化深度限制**
  - 文件: `Mud.Feishu.Webhook/Configuration/FeishuJsonOptions.cs`
  - 问题: 缺少 MaxDepth 限制，可能导致性能问题
  - 修复: 添加 MaxDepth = 64 限制
  - 影响: 反序列化安全

- **限流内存管理优化**
  - 文件: `Mud.Feishu.Webhook/Middleware/FeishuRateLimitMiddleware.cs`
  - 问题: 字典可能无限增长，缺少容量上限
  - 修复: 添加最大条目限制(10万)和 LRU 淘汰
  - 影响: 限流中间件稳定性

- **并发控制资源管理**
  - 文件: `Mud.Feishu.Webhook/Services/FeishuWebhookConcurrencyService.cs`
  - 问题: Task.Run 未传递取消令牌，可能无法正确释放
  - 修复: 实现 IHostedService，添加应用停止钩子
  - 影响: 服务关闭时的资源释放

### 🐛 Bug 修复

#### 资源管理修复

- **AES/SHA256 资源泄漏**
  - 文件:
    - `Mud.Feishu.Webhook/Services/FeishuEventDecryptor.cs`
    - `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - 修复: 使用 `using` 语句确保资源释放
  - 影响: 加密/解密操作

- **并发控制资源泄漏**
  - 文件: `Mud.Feishu.Webhook/Services/FeishuWebhookConcurrencyService.cs`
  - 修复: 实现 IHostedService 生命周期管理
  - 影响: 服务关闭时的资源释放

#### 逻辑修复

- **配置变更事件通知**
  - 文件: `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
  - 修复: 添加 OptionsChanged 事件
  - 影响: 配置热更新的实时通知

### 📝 文档改进

#### 新增文档

- **docs/troubleshooting.md**
  - 故障排查指南
  - 常见问题解答(FAQ)
  - 诊断流程图

- **docs/performance-tuning.md**
  - 性能调优指南
  - 并发数配置建议
  - 监控指标说明

- **docs/security-best-practices.md**
  - 安全最佳实践
  - 生产环境配置清单
  - 风险评估建议

#### 改进文档

- **README.md**
  - 更新配置说明
  - 添加故障排查链接
  - 补充性能优化建议

- **SECURITY.md**
  - 更新安全配置要求
  - 添加 CVE 披露流程
  - 补充安全审计报告

### 🔄 破坏性变更

#### API 变更

- **IFeishuEventHandlerFactory 扩展**
  - 新增方法: `GetHandlerInfo()`, `ClearHandlers()`
  - 影响: 需要实现接口的类

- **FeishuWebhookOptions 扩展**
  - 新增属性: `MaxRetryCount`, `CircuitBreakerEnabled`
  - 影响: 配置文件需要更新

#### 配置变更

- **新增必需配置**
  - `MaxRetryCount`: 默认值 3
  - `CircuitBreakerEnabled`: 默认值 true
  - `MaxRequestBodySize`: 默认值 10MB

### ⚠️ 迁移指南

如果您正在升级到 v1.2.0，请更新配置文件：

```json
{
  "FeishuWebhook": {
    "EnforceHeaderSignatureValidation": true,
    "TimestampToleranceSeconds": 60,
    "MaxConcurrentEvents": 10,
    "EventHandlingTimeoutMs": 30000,
    "EnableBackgroundProcessing": false,
    "MaxRetryCount": 3,
    "CircuitBreakerEnabled": true,
    "RateLimit": {
      "EnableRateLimit": true,
      "MaxRequestsPerWindow": 100,
      "WindowSizeSeconds": 60,
      "MaxIpEntries": 100000
    }
  }
}
```

如果使用拦截器，需要在 `Program.cs` 中注册：

```csharp
builder.Services.AddFeishuWebhook(options =>
{
    // ... 现有配置 ...
})
.AddEventInterceptor<LoggingEventInterceptor>()
.AddEventInterceptor<MetricEventInterceptor>();
```

#### 依赖项更新

新增依赖:
- `Polly`: 用于断路器模式

```bash
dotnet add package Polly
```

### 🧪 测试

#### 新增单元测试

- Nonce 去重清理测试
- Content-Type 验证测试
- JSON 深度限制测试
- 流式请求体读取测试
- 拦截器执行顺序测试
- 断路器状态转换测试
- 指数退避重试测试
- 日志脱敏测试
- 限流 LRU 淘汰测试
- 并发控制资源释放测试

#### 新增集成测试

- 端到端 Webhook 请求测试
- 失败事件重试流程测试
- 断路器故障恢复测试
- 分布式场景测试(多实例)

#### 性能测试

- 10万并发请求测试
- 1MB 请求体处理测试
- 深度嵌套 JSON 处理测试
- 内存泄漏压力测试

### 🔄 Mud.Feishu.WebSocket 模块增强

#### 错误处理增强

- **错误分类处理**
  - 文件: `Mud.Feishu.WebSocket/FeishuWebSocketClient.cs`
  - 新增: 区分可恢复和不可恢复错误
  - 修复: 详细的异常分类处理，帮助快速定位问题
  - 影响: WebSocket 连接稳定性

- **认证失败详细追踪**
  - 文件: `Mud.Feishu.WebSocket/Core/AuthenticationManager.cs`
  - 新增: 按错误码分类认证失败原因，统计失败次数和时间
  - 修复: 详细的错误码分类处理和统计信息
  - 影响: WebSocket 认证问题排查

- **资源管理优化**
  - 文件: `Mud.Feishu.WebSocket/FeishuWebSocketHostedService.cs`
  - 修复: 实现 IHostedService 生命周期管理，避免资源泄漏
  - 影响: 服务关闭时的资源释放

#### 新增配置选项

- `EnableDetailedErrorTracking`: 启用详细错误跟踪，默认值 false
- `MaxAuthenticationFailureCount`: 最大认证失败次数，默认值 5
- `AuthenticationFailureWindowMinutes`: 认证失败统计窗口(分钟)，默认值 10

### 🔄 Mud.Feishu.WebHook 模块增强

#### 安全性增强

- **内容类型验证**
  - 文件: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - 新增: 请求 Content-Type 验证，仅接受 `application/json`
  - 防止: 恶意构造的非 JSON 请求
  - 影响: 提升请求安全性

- **JSON 深度限制**
  - 文件: `Mud.Feishu.Webhook/Configuration/FeishuJsonOptions.cs`
  - 新增: `MaxDepth = 64` 限制，防止深度嵌套 JSON
  - 防止: DoS 攻击和栈溢出风险
  - 影响: 反序列化安全

- **流式请求体读取**
  - 文件: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - 优化: 流式读取请求体，实时验证大小
  - 防止: 伪造 Content-Length 的 DoS 攻击
  - 影响: 内存使用优化

- **Nonce 过期清理**
  - 文件: `Mud.Feishu.Abstractions/IFeishuNonceDistributedDeduplicator.cs`
  - 修复: 添加基于时间戳的 TTL 清理机制
  - 防止: 内存泄漏
  - 影响: 所有使用 Nonce 去重的场景

#### 性能优化

- **断路器模式**
  - 文件: `Mud.Feishu.Webhook/Services/FeishuWebhookService.cs`
  - 新增: 使用 Polly 实现断路器模式
  - 配置: 连续 5 次失败后断开，30 秒后重试
  - 影响: 提升系统稳定性

- **失败事件重试**
  - 文件: `Mud.Feishu.Webhook/Services/FailedEventRetryService.cs`
  - 新增: 后台自动重试失败事件
  - 策略: 指数退避(2^retryCount 分钟, 最大 60 分钟)
  - 影响: 提高事件处理可靠性

- **事件处理拦截器**
  - 文件: `Mud.Feishu.Abstractions/IFeishuEventInterceptor.cs`
  - 新增: 前置/后置事件处理拦截器机制
  - 支持: 日志记录、性能监控、自定义验证
  - 影响: 提升可扩展性

#### 可观测性增强

- **日志脱敏**
  - 文件: `Mud.Feishu.Webhook/Utils/LogSanitizer.cs`
  - 新增: 自动脱敏敏感字段(encrypt, signature, token 等)
  - 防止: 敏感信息泄露到日志
  - 影响: 生产环境日志安全

- **扩展指标收集**
  - 文件: `Mud.Feishu.Webhook/Models/MetricsCollector.cs`
  - 新增: 更完善的监控指标
  - 影响: 更好的可观测性

#### 新增配置选项

- `MaxRetryCount`: 最大重试次数，默认值 3
- `CircuitBreakerEnabled`: 是否启用断路器，默认值 true
- `MaxDepth`: JSON 最大解析深度，默认值 64
- `MaxIpEntries`: 限流中间件最大 IP 条目数，默认值 100000

### 🔄 Mud.Feishu.Redis 模块增强

#### 降级策略

- **Redis 连接失败降级处理**
  - 文件: `Mud.Feishu.Redis/Services/RedisFeishuEventDistributedDeduplicatorWithFallback.cs`（新增）
  - 问题: Redis 连接失败时没有降级策略
  - 修复: 实现自动降级到内存去重，支持指数退避重试
  - 影响: 使用 Redis 分布式去重的用户

### 📦 依赖更新

#### 新增依赖

- `Polly`: 8.4.0
  - 用途: 断路器模式实现
  - 许可证: BSD-3-Clause

#### 更新依赖

- `Microsoft.Extensions.Diagnostics.HealthChecks`: 8.0.x
- `Microsoft.Extensions.Logging.Abstractions`: 8.0.x
- `System.Text.Json`: 8.0.x

---

## 1.1.3 (2026-01-15)

**类型**: Bug 修复和安全增强

### 🔒 安全修复

#### 高危问题修复

- **[CVE-2026-XXXX] 生产环境签名验证检查恢复**
  - 文件: `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
  - 问题: 生产环境强制签名验证逻辑被注释，存在严重安全风险
  - 修复: 取消注释第 197-202 行，确保生产环境必须启用签名验证
  - 影响: 所有的 Webhook 使用者

- **[CVE-2026-XXXX] 后台处理失败事件持久化**
  - 文件: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - 问题: 后台处理失败时，条件判断错误导致失败事件无法被持久化
  - 修复: 修正第 583 行条件判断，实现 TODO 注释（第 601-602 行）
  - 影响: 启用后台处理模式的所有用户

- **[CVE-2026-XXXX] Demo 项目硬编码敏感密钥**
  - 文件: `Mud.Feishu.Webhook.Demo/appsettings.json`
  - 问题: VerificationToken 和 EncryptKey 硬编码在配置文件中
  - 修复: 移除硬编码值，添加 SECURITY-WARNING.md 说明文档
  - 影响: Demo 项目用户

#### 中危问题修复

- **Redis 连接失败降级处理**
  - 文件: `Mud.Feishu.Redis/Services/RedisFeishuEventDistributedDeduplicatorWithFallback.cs`（新增）
  - 问题: Redis 连接失败时没有降级策略
  - 修复: 实现自动降级到内存去重，支持指数退避重试
  - 影响: 使用 Redis 分布式去重的用户

- **AES/SHA256 资源泄漏修复**
  - 文件:
    - `Mud.Feishu.Webhook/Services/FeishuEventDecryptor.cs`
    - `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - 问题: AES 和 SHA256 实例未正确释放，可能导致资源泄漏
  - 修复: 使用 `using` 语句确保资源正确释放
  - 影响: 所有使用事件解密的场景

- **InMemoryFailedEventStore 线程安全问题**
  - 文件: `Mud.Feishu.Webhook/Services/InMemoryFailedEventStore.cs`
  - 问题: 清理方法未加锁，与字典操作存在竞态条件
  - 修复: 在 `CleanupExpiredEvents` 方法中添加锁保护
  - 影响: 使用内存失败事件存储的用户

- **InMemoryFailedEventStore Timer 资源泄漏**
  - 文件: `Mud.Feishu.Webhook/Services/InMemoryFailedEventStore.cs`
  - 问题: Timer 实例未在 Dispose 方法中释放
  - 修复: 实现 IDisposable 接口，在 Dispose 中释放 Timer
  - 影响: 使用内存失败事件存储的用户

- **FeishuSeqIDDeduplicator 缓存清理逻辑**
  - 文件: `Mud.Feishu.Abstractions/Services/FeishuSeqIDDeduplicator.cs`
  - 问题: 最大 SeqID 计算逻辑不完整，可能导致重复处理
  - 修复: 在清理完成后统一计算最大 SeqID
  - 影响: 使用 SeqID 去重的用户

- **日志敏感信息清理**
  - 文件: `Mud.Feishu.Webhook/Services/FeishuEventDecryptor.cs`
  - 问题: 日志中包含完整的解密 JSON 数据，可能泄露敏感信息
  - 修复: 仅记录事件数据长度，不记录完整内容
  - 影响: 所有的日志使用者

- **日志 Emoji 符号移除**
  - 文件: `Mud.Feishu.Webhook/Middleware/FeishuWebhookMiddleware.cs`
  - 问题: 日志使用 emoji 表情符号，生产环境可能造成日志解析问题
  - 修复: 移除日志中的 emoji 符号
  - 影响: 生产环境日志解析

- **Nonce 去重逻辑注释优化**
  - 文件: `Mud.Feishu.Webhook/Services/FeishuEventValidator.cs`
  - 问题: 代码逻辑正确但缺少清晰注释
  - 修复: 添加详细注释说明返回值语义
  - 影响: 代码可读性

- **IFeishuEventHandlerFactory 接口完善**
  - 文件: `Mud.Feishu.Abstractions/IFeishuEventHandlerFactory.cs`
  - 问题: 接口缺少 `UnregisterHandler(IFeishuEventHandler handler)` 方法定义
  - 修复: 添加接口方法定义，与实现类保持一致
  - 影响: 需要取消注册特定事件处理器的用户

- **重复服务注册修复**
  - 文件: `Mud.Feishu.Webhook/Extensions/FeishuWebhookServiceBuilder.cs`
  - 问题: `IFeishuNonceDistributedDeduplicator` 被重复注册
  - 修复: 移除第 296 行的重复注册
  - 影响: 所有使用 Webhook 服务的用户

### 🐛 Bug 修复

#### 核心功能修复

- **后台处理模式失败事件存储**
  - 修复后台处理模式下失败事件无法被持久化的问题
  - 正确调用 `IFailedEventStore.StoreFailedEventAsync`
  - 添加详细的错误日志记录

#### 资源管理修复

- **AES 加密资源释放**
  - 使用 `using var` 语句确保 AES 实例正确释放
  - 同时释放 ICryptoTransform 资源

- **SHA256 哈希资源释放**
  - 使用 `using` 语句确保 SHA256 实例正确释放

- **Timer 资源释放**
  - InMemoryFailedEventStore 实现 IDisposable
  - 确保定时清理 Timer 正确释放

#### 线程安全修复

- **失败事件存储清理方法**
  - 添加锁保护清理操作
  - 确保与字典操作并发安全

#### 逻辑修复

- **SeqID 去重清理**
  - 统一在清理完成后重新计算最大 SeqID
  - 避免部分清理导致的计算错误

#### 新增功能

- **Redis 降级去重器**
  - 新增 `RedisFeishuEventDistributedDeduplicatorWithFallback` 类
  - 支持自动降级到内存去重
  - 指数退避重试机制
  - 状态查询和监控能力

### 📝 文档改进

#### 新增文档

- **SECURITY-WARNING.md**
  - Demo 项目安全配置指南
  - 生产环境安全检查清单
  - 环境变量配置说明

#### 改进文档

- **README.md**
  - 更新安全配置建议
  - 添加最佳实践链接

### 🔄 破坏性变更

#### 配置变更

- **生产环境签名验证**
  - 之前: 生产环境签名验证可以被禁用
  - 现在: 生产环境强制启用签名验证，禁用将抛出异常

- **Demo 项目配置**
  - 之前: appsettings.json 包含硬编码的示例密钥
  - 现在: 配置文件为空，必须通过环境变量或手动配置

### ⚠️ 迁移指南

#### 生产环境配置

如果您在生产环境使用了以下配置，需要进行相应的调整：

##### 1. 启用签名验证

```json
{
  "FeishuWebhook": {
    "EnforceHeaderSignatureValidation": true,
    "EnableBodySignatureValidation": true
  }
}
```

##### 2. 配置环境变量

```bash
# Linux/macOS
export FeishuWebhook__VerificationToken="your_verification_token"
export FeishuWebhook__EncryptKey="your_32_byte_encryption_key"

# Windows PowerShell
$env:FeishuWebhook__VerificationToken="your_verification_token"
$env:FeishuWebhook__EncryptKey="your_32_byte_encryption_key"
```

##### 3. 验证配置

部署前请确认：
- [ ] 签名验证已启用
- [ ] 时间戳容错范围 <= 60 秒
- [ ] EncryptKey 为 32 字节强密钥
- [ ] IP 验证已启用并配置白名单
- [ ] 日志不包含敏感信息

### 🧪 测试

#### 新增测试

- 后台处理失败事件存储测试
- AES/SHA256 资源释放测试
- InMemoryFailedEventStore 线程安全测试
- SeqID 去重清理逻辑测试
- Timer 资源释放测试
- IFeishuEventHandlerFactory 接口方法测试
- WebSocket 错误分类测试
- Redis 降级策略测试
- 认证失败处理测试

---

## 1.1.0 (2025-12-31)

**FEATURES**

### 🔧 核心优化与重构

- 📦 **多框架支持**: 支持 .NET Standard 2.0、.NET 6.0、.NET 8.0、.NET 10.0
  - 提供跨平台兼容性，支持从 .NET Framework 4.6+ 到 .NET 10.0
  - 统一 API 接口，不同框架版本使用相同的编程模型
  - 自动编译时条件处理，充分利用各平台特性

- 🏗️ **响应类型统一**: 更新所有 API 响应类型为 `FeishuApiResult<T>` 系列
  - `FeishuApiResult<T>` - 通用响应类型
  - `FeishuApiPageListResult<T>` - 分页列表响应
  - `FeishuApiListResult<T>` - 列表响应
  - `FeishuNullDataApiResult` - 空数据响应

- 🔧 **消息发送接口重构**: 统一消息发送接口设计
  - `SendMessageRequest` 替代 `TextMessageRequest`，支持所有消息类型
  - `MessageTextContent` 替代 `TextContent`，保持类型一致性
  - 改进 Content 字段序列化机制

### 📋 新增审批 API 支持

- ✅ **审批定义管理**
  - `IFeishuV4Approval` - V4 审批基础接口
  - `IFeishuTenantV4Approval` - V4 租户审批接口
  - 支持创建审批定义、查询审批实例等核心功能

### 📝 任务管理增强

- 🎯 **自定义字段管理**
  - 创建、更新、查询自定义字段
  - 自定义字段选项管理
  - 自定义字段资源绑定
  - 分页查询自定义字段列表

- 📊 **任务分组管理**
  - 创建、更新、删除任务分组
  - 查询任务分组列表
  - 任务分组资源绑定

### 🔄 WebSocket 实时事件订阅

- 🌐 **飞书 WebSocket 客户端** (`Mud.Feishu.WebSocket`)
  - 支持飞书 WebSocket 实时事件订阅
  - 自动重连机制，保证连接稳定性
  - 心跳检测，及时发现连接异常
  - 二进制消息解析，支持完整事件类型

### 📡 Webhook 事件处理

- 🎭 **事件处理器抽象层** (`Mud.Feishu.Abstractions`)
  - 策略模式架构，灵活扩展事件处理器
  - 工厂模式管理，自动发现和注册处理器
  - 完整的飞书事件类型覆盖：
    - 用户事件：创建、更新、离职、状态变更
    - 部门事件：创建、更新、删除
    - 员工事件：入职、离职、信息变更
    - 消息事件：接收、发送状态、阅读状态
    - 任务事件：创建、更新、删除、状态变更
    - 审批事件：提交、通过、拒绝、撤销

### 🎨 消息和卡片增强

- 📰 **消息流卡片 API**
  - 应用消息流卡片完整接口支持
  - 卡片实体组件管理
  - 卡片内容更新和删除

- 💬 **群组功能增强**
  - 群公告管理
  - 会话标签页管理
  - 群组自定义菜单设置

### 🛠️ 配置和工具优化

- ⚙️ **配置增强**
  - 添加 `FeishuOptions` 配置类
  - 支持配置文件绑定
  - 日志配置选项

- 🔒 **安全增强**
  - URL 验证功能，防止恶意请求
  - Authorization 头常量统一管理

**REFACTOR**

- 📁 **命名空间统一**
  - 统一接口命名空间结构
  - 全局引用导入，简化代码

- 🧹 **代码清理**
  - 移除废弃的 TaskSectionsResult 类
  - 移除无效的类和接口
  - 统一服务注册 API，移除废弃的 `UseMultiHandler` 方法

**BUG FIX**

- 🔧 **修复 HttpClient 配置问题**
- 🔧 **修复 API 端点 URL 格式问题**
- 🔧 **修复消息和任务附件 API 实现**

**DOCS**

- 📚 **文档更新**
  - 更新 README 文档结构
  - 移除冗余的架构设计和性能特性文档
  - 添加部门事件处理器文档和使用示例
  - 优化项目描述和功能说明

---

## 1.0.2 (2025-11-26)

**FEATURES**

- 🏗️ **重构优化**: 创建 `ChatGroupBase` 基类，整合聊天群组相关通用属性
  - 减少 70+ 个重复属性，提升代码复用性
  - 统一 `GetChatGroupInfoResult`、`CreateUpdateChatResult`、`UpdateChatRequest`、`CreateChatRequest` 类结构
  - 保持完整的 JsonPropertyName 特性，确保 JSON 序列化兼容性

- 📚 **文档完善**: 为所有聊天群组和群组成员相关类添加完整的 XML 文档注释
  - `ChatGroupModeratorPageListResult` - 聊天群组管理员分页列表结果
  - `ChatItemInfo` - 聊天项目基本信息
  - `ShareLinkDataResult` - 分享链接数据结果
  - `AddMemberResult` - 添加成员操作结果
  - `GetMemberIsInChatResult` - 成员群组状态查询结果
  - `GetMemberPageListResult` - 群组成员分页列表结果
  - `RemoveMemberResult` - 移除成员操作结果
  - `GroupManagerResult` - 群管理员操作结果

- 🎯 **代码质量**: 提升代码可读性和维护性
  - 所有新增注释遵循 C# XML 文档规范
  - 包含详细的业务含义和使用场景说明
  - 区分不同参数值的实际效果

## 1.0.1 (2025-11-20)

**REFACTOR**

- 优化依赖注入配置结构
- 改进令牌管理器的并发安全性
- 重构 HTTP 客户端工厂配置

**FEATURES**

- 增强错误处理机制
- 添加详细的日志记录支持
- 支持自定义 HTTP 头配置

**BUG FIX**

- 修复令牌刷新时的并发问题
- 解决分页查询中的数据丢失问题
- 修复批量消息发送的状态追踪错误


### 📱 消息服务
- **多类型消息**: 文本、图片、文件、卡片等丰富消息类型
- **批量发送**: 支持批量消息发送和状态追踪
- **消息互动**: 表情回复、消息撤回、已读回执
- **异步处理**: 完全异步的消息处理机制

---

## 1.0.0 (2025-11-01)

### 🎉 首次发布 - Mud.Feishu 飞书 API SDK

**FEATURES**

### 🔐 认证授权系统
- **多重令牌管理**: 支持应用令牌、租户令牌、用户令牌
- **自动刷新机制**: 智能令牌刷新，提前 5 分钟触发更新
- **高并发安全**: 使用 `ConcurrentDictionary` 和 `Lazy<Task>` 避免缓存击穿
- **OAuth 授权流程**: 完整支持飞书 OAuth 2.0 授权


### 🏢 组织架构管理
#### 用户管理 (V1/V3)
- **用户 CRUD**: 创建、查询、更新、删除用户
- **批量操作**: 批量获取用户信息、批量状态更新
- **部门关联**: 用户与部门的多对多关系管理
- **搜索过滤**: 支持多种搜索条件和分页

#### 部门管理 (V1/V3)
- **树形结构**: 支持无限层级的部门树
- **递归查询**: 递归获取子部门和成员
- **权限继承**: 部门权限自动继承机制

#### 员工管理 (V1)
- **员工信息**: 员工详细信息管理
- **入职离职**: 员工入职和离职流程支持

#### 用户组管理 (V3)
- **用户组 CRUD**: 创建、查询、更新、删除用户组
- **成员管理**: 用户组成员的添加、移除、查询
- **权限分配**: 基于用户组的权限控制

### 🏢 企业管理体系
#### 人员类型管理 (V3)
- **分类体系**: 员工类型分类和标签管理
- **灵活配置**: 支持自定义人员类型属性

#### 职级管理 (V3)
- **职级体系**: 完整的职级晋升和管理
- **职级关联**: 与薪资、权限的关联配置

#### 职位序列管理 (V3)
- **职业路径**: 员工职业发展路径管理
- **序列定义**: 不同序列的职位定义

#### 职务管理 (V3)
- **职务定义**: 具体职务的职责和权限定义
- **职务分配**: 员工职务的分配和变更

#### 角色管理 (V3)
- **权限角色**: 基于角色的访问控制 (RBAC)
- **角色继承**: 角色权限的继承和组合
- **成员管理**: 角色成员的添加、移除操作

#### 单位管理 (V3)
- **组织单位**: 企业组织单位的管理
- **单位层级**: 单位之间的层级关系

#### 工作城市管理 (V3)
- **办公地点**: 工作城市和地点管理
- **地点关联**: 与部门、员工的关联关系

### 🔧 核心技术特性

#### 特性驱动设计
- **[HttpClientApi] 特性**: 自动生成 HTTP 客户端代码
- **强类型支持**: 编译时类型检查，减少运行时错误
- **统一响应**: 基于 `FeishuApiResult<T>` 的统一响应处理

#### 依赖注入友好
- **服务注册**: `AddFeishuApiService()` 扩展方法
- **配置灵活**: 支持配置文件和代码配置
- **生命周期管理**: 自动管理服务生命周期

#### 高性能缓存
- **智能缓存**: 令牌自动缓存和刷新
- **并发控制**: 解决高并发下的缓存问题
- **资源管理**: 实现 `IDisposable` 接口

#### 异常处理
- **统一异常**: `FeishuException` 统一异常处理
- **错误分类**: 不同类型错误的分类处理
- **日志集成**: 与 .NET 日志系统集成

### 🌐 API 覆盖范围

#### 认证授权 API
- `IFeishuV3AuthenticationApi` - V3 认证授权接口

#### 消息服务 API
- `IFeishuV1Message` - V1 消息基础接口
- `IFeishuTenantV1Message` - V1 租户消息接口
- `IFeishuUserV1Message` - V1 用户消息接口
- `IFeishuTenantV1BatchMessage` - V1 批量消息接口

#### 组织架构 API (V1)
- `IFeishuV1ChatGroup` - V1 聊天群组基础接口
- `IFeishuTenantV1ChatGroup` - V1 租户聊天群组接口
- `IFeishuUserV1ChatGroup` - V1 用户聊天群组接口
- `IFeishuV1ChatGroupMember` - V1 聊天群组成员基础接口
- `IFeishuTenantV1ChatGroupMember` - V1 租户聊天群组成员接口
- `IFeishuUserV1ChatGroupMember` - V1 用户聊天群组成员接口
- `IFeishuV1Departments` - V1 部门管理基础接口
- `IFeishuTenantV1Departments` - V1 租户部门管理接口
- `IFeishuUserV1Departments` - V1 用户部门管理接口
- `IFeishuV1Employees` - V1 员工管理基础接口
- `IFeishuTenantV1Employees` - V1 租户员工管理接口
- `IFeishuUserV1Employees` - V1 用户员工管理接口

#### 企业管理 API (V3)
- `IFeishuV3Departments` - V3 部门管理基础接口
- `IFeishuTenantV3Departments` - V3 租户部门管理接口
- `IFeishuUserV3Departments` - V3 用户部门管理接口
- `IFeishuTenantV3EmployeeType` - V3 租户人员类型管理接口
- `IFeishuTenantV3JobFamilies` - V3 租户职位序列管理接口
- `IFeishuTenantV3JobLevel` - V3 租户职级管理接口
- `IFeishuV3JobTitle` - V3 职务管理基础接口
- `IFeishuTenantV3JobTitle` - V3 租户职务管理接口
- `IFeishuUserV3JobTitle` - V3 用户职务管理接口
- `IFeishuTenantV3RoleMember` - V3 租户角色成员管理接口
- `IFeishuTenantV3Role` - V3 租户角色管理接口
- `IFeishuTenantV3Unit` - V3 租户单位管理接口
- `IFeishuV3User` - V3 用户管理基础接口
- `IFeishuTenantV3User` - V3 租户用户管理接口
- `IFeishuUserV3User` - V3 用户管理接口
- `IFeishuTenantV3UserGroupMember` - V3 租户用户组成员管理接口
- `IFeishuTenantV3UserGroup` - V3 租户用户组管理接口
- `IFeishuTenantV3WorkCity` - V3 租户工作城市管理接口
- `IFeishuV3WorkCity` - V3 工作城市基础接口

### 📦 技术栈

#### 框架支持
- **.NET Standard 2.0** - 兼容 .NET Framework 4.6.1+
- **.NET 6.0** - LTS 长期支持版本
- **.NET 8.0** - LTS 长期支持版本 
- **.NET 10.0** - LTS 长期支持版本

#### 核心依赖
- **Mud.ServiceCodeGenerator v1.4.5.3** - HTTP 客户端代码生成器
- **System.Text.Json v10.0.1** - 高性能 JSON 序列化 (.NET Standard 2.0)
- **Microsoft.Extensions.Http** - HTTP 客户端工厂
  - .NET 6.0 / .NET Standard 2.0: v8.0.1
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Http.Polly** - 弹性和瞬态故障处理
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.DependencyInjection** - 依赖注入
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Logging** - 日志记录
  - .NET 6.0 / .NET Standard 2.0: v8.0.3
  - .NET 8.0 / .NET 10.0: v10.0.1
- **Microsoft.Extensions.Configuration.Binder** - 配置绑定
  - .NET 6.0 / .NET Standard 2.0: v8.0.2
  - .NET 8.0 / .NET 10.0: v10.0.1

## 🔗 相关链接

- [项目Gitee主页](https://gitee.com/mudtools/MudFeishu)
- [项目Github主页](https://github.com/mudtools/MudFeishu)
- [NuGet 包](https://www.nuget.org/packages/Mud.Feishu/)
- [文档网站](https://www.mudtools.cn/documents/guides/feishu/)
- [飞书开放平台](https://open.feishu.cn/document/)
- [问题反馈](https://gitee.com/mudtools/MudFeishu/issues)

---

*注意：本 CHANGELOG 遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/) 规范。*
