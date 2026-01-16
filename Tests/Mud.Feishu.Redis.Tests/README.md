# Mud.Feishu.Redis 测试项目

本项目包含 Mud.Feishu.Redis 组件的单元测试，用于验证 Redis 分布式去重功能的正确性和可靠性。

## 测试覆盖范围

### 1. 服务测试 (Services)

#### RedisFeishuEventDistributedDeduplicatorTests
测试基于 Redis 的分布式事件去重服务：
- ✅ 首次事件标记（应返回 false，表示未处理过）
- ✅ 重复事件检测（应返回 true，表示已处理过）
- ✅ Redis 连接失败处理（应抛出 InvalidOperationException）
- ✅ 事件处理状态查询
- ✅ 空事件 ID 处理
- ✅ 自定义 TTL 支持
- ✅ 手动移除事件标记
- ✅ 过期清理（Redis 自动清理）

#### RedisFeishuNonceDistributedDeduplicatorTests
测试基于 Redis 的分布式 Nonce 去重服务（防重放攻击）：
- ✅ 首次 Nonce 标记
- ✅ 重复 Nonce 检测（防重放攻击）
- ✅ 空 Nonce 处理
- ✅ 自定义 TTL 支持
- ✅ Nonce 使用状态查询
- ✅ Redis 连接失败处理
- ✅ Redis 超时处理
- ✅ 手动移除 Nonce 标记
- ✅ 过期清理

#### RedisFeishuSeqIDDeduplicatorTests
测试基于 Redis 的分布式 SeqID 去重服务：
- ✅ 首次 SeqID 标记（同步和异步）
- ✅ 重复 SeqID 检测（同步和异步）
- ✅ SeqID 处理状态查询（同步和异步）
- ✅ 获取缓存数量
- ✅ 获取最大已处理 SeqID
- ✅ 无 SeqID 时返回零
- ✅ Redis 连接失败处理（同步和异步）

#### RedisFeishuEventDistributedDeduplicatorWithFallbackTests
测试带降级策略的分布式事件去重服务：
- ✅ Redis 可用时使用 Redis
- ✅ Redis 失败时自动降级到内存
- ✅ 连续失败后标记 Redis 不可用
- ✅ Redis 恢复后重置失败计数
- ✅ 查询操作的降级处理
- ✅ 空事件 ID 处理
- ✅ 超时重试机制（指数退避）
- ✅ 过期清理（清理降级去重器）
- ✅ Redis 不可用时直接使用降级

### 2. 健康检查测试 (Health)

#### RedisHealthCheckTests
测试 Redis 健康检查功能：
- ✅ Redis 健康时返回 Healthy 状态
- ✅ Redis 响应慢时返回 Degraded 状态
- ✅ Redis 连接失败时返回 Unhealthy 状态
- ✅ Redis 超时时返回 Unhealthy 状态

### 3. 配置测试 (Configuration)

#### RedisOptionsTests
测试 Redis 配置选项：
- ✅ 默认值验证
- ✅ 自定义值设置
- ✅ 不同格式的服务器地址
- ✅ 自定义键前缀
- ✅ 超时配置
- ✅ 缓存过期时间配置

## 测试技术栈

- **测试框架**: xUnit 2.9.2
- **Mock 框架**: Moq 4.20.72
- **测试运行器**: Microsoft.NET.Test.Sdk 17.11.1
- **代码覆盖**: coverlet.collector 6.0.2

## 运行测试

### 使用 .NET CLI
```bash
# 运行所有测试
dotnet test Tests/Mud.Feishu.Redis.Tests/Mud.Feishu.Redis.Tests.csproj

# 运行测试并生成代码覆盖率报告
dotnet test Tests/Mud.Feishu.Redis.Tests/Mud.Feishu.Redis.Tests.csproj --collect:"XPlat Code Coverage"

# 运行特定测试类
dotnet test Tests/Mud.Feishu.Redis.Tests/Mud.Feishu.Redis.Tests.csproj --filter "FullyQualifiedName~RedisFeishuEventDistributedDeduplicatorTests"
```

### 使用 Visual Studio
1. 打开测试资源管理器（Test Explorer）
2. 点击"运行所有测试"按钮
3. 查看测试结果和覆盖率

## 测试结构

```
Tests/Mud.Feishu.Redis.Tests/
├── Configuration/
│   └── RedisOptionsTests.cs              # 配置选项测试
├── Health/
│   └── RedisHealthCheckTests.cs          # 健康检查测试
├── Services/
│   ├── RedisFeishuEventDistributedDeduplicatorTests.cs              # 事件去重测试
│   ├── RedisFeishuNonceDistributedDeduplicatorTests.cs              # Nonce 去重测试
│   ├── RedisFeishuSeqIDDeduplicatorTests.cs                         # SeqID 去重测试
│   └── RedisFeishuEventDistributedDeduplicatorWithFallbackTests.cs  # 降级策略测试
├── GlobalUsings.cs                       # 全局引用
├── Mud.Feishu.Redis.Tests.csproj        # 项目文件
└── README.md                             # 本文档
```

## 测试原则

1. **单元测试隔离**: 使用 Mock 对象隔离外部依赖（Redis 连接）
2. **AAA 模式**: 所有测试遵循 Arrange-Act-Assert 模式
3. **命名规范**: 测试方法命名格式为 `MethodName_Scenario_ExpectedBehavior`
4. **完整覆盖**: 覆盖正常流程、异常流程和边界条件
5. **快速执行**: 单元测试不依赖真实 Redis 实例，执行速度快

## 注意事项

- 所有测试使用 Mock 对象，不需要真实的 Redis 服务器
- 测试覆盖了同步和异步方法
- 测试验证了异常处理和降级策略
- 健康检查测试验证了不同的健康状态

## 贡献指南

添加新测试时，请遵循以下规范：
1. 使用清晰的测试方法命名
2. 添加必要的注释说明测试目的
3. 确保测试独立且可重复执行
4. 验证正常和异常情况
5. 更新本 README 文档

## 许可证

本项目遵循 MIT 许可证。详见根目录的 LICENSE 文件。
