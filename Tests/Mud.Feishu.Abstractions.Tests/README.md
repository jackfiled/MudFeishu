# Mud.Feishu.Abstractions.Tests

Mud.Feishu.Abstractions 项目的单元测试套件。

## 📋 测试覆盖范围

### 事件处理器测试 (EventHandlers)
- ✅ `DefaultFeishuEventHandlerTests` - 默认事件处理器基类测试
- ✅ `DefaultFeishuEventHandlerFactoryTests` - 事件处理器工厂测试
- ✅ `IdempotentFeishuEventHandlerTests` - 幂等性事件处理器测试
- ✅ `DefaultFeishuObjectEventHandlerTests` - 对象事件处理器测试

### 服务测试 (Services)
- ✅ `FeishuEventDeduplicatorTests` - 事件去重服务测试
- ✅ `FeishuEventDistributedDeduplicatorTests` - 分布式事件去重测试
- ✅ `FeishuSeqIDDeduplicatorTests` - SeqID去重测试
- ✅ `FeishuNonceDistributedDeduplicatorTests` - Nonce去重测试

### 拦截器测试 (Interceptors)
- ✅ `LoggingEventInterceptorTests` - 日志拦截器测试
- ✅ `TelemetryEventInterceptorTests` - 遥测拦截器测试

### 认证测试 (Authentication)
- ✅ `AppTokenManagerTests` - 应用令牌管理器测试
- ✅ `TenantTokenManagerTests` - 租户令牌管理器测试
- ✅ `UserTokenManagerTests` - 用户令牌管理器测试
- ✅ `TokenManagerWithCacheTests` - 带缓存的令牌管理器测试

## 🚀 运行测试

### 运行所有测试
```bash
dotnet test
```

### 运行特定测试类
```bash
dotnet test --filter "FullyQualifiedName~DefaultFeishuEventHandlerTests"
```

### 生成测试覆盖率报告
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 测试统计

- 总测试用例数: 100+
- 测试覆盖率目标: > 85%
- 测试框架: xUnit
- Mock 框架: Moq

## 🔧 测试原则

1. **单一职责** - 每个测试只验证一个功能点
2. **独立性** - 测试之间互不依赖
3. **可重复性** - 测试结果稳定可靠
4. **清晰命名** - 测试方法名清楚表达测试意图
5. **AAA模式** - Arrange-Act-Assert 结构

## 📝 测试命名规范

```
MethodName_Scenario_ExpectedBehavior
```

示例:
- `HandleAsync_WhenEventDataIsValid_ShouldCallProcessBusinessLogic`
- `TryMarkAsProcessed_WhenDuplicateEvent_ShouldReturnTrue`

## 🛠️ 依赖项

- xUnit - 测试框架
- Moq - Mock 框架
- Microsoft.Extensions.Logging - 日志抽象

## 📚 相关文档

- [Mud.Feishu.Abstractions README](../../Mud.Feishu.Abstractions/README.md)
- [测试最佳实践](../../docs/testing-best-practices.md)
