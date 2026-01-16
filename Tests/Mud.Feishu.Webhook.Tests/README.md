# Mud.Feishu.Webhook 测试项目

本项目包含 Mud.Feishu.Webhook 组件的单元测试，用于验证 Webhook 事件处理功能的正确性和可靠性。

## 测试覆盖范围

### 1. 服务测试 (Services)

#### FeishuEventValidatorTests
测试飞书事件验证服务：
- 订阅请求验证（有效/无效 Token）
- 订阅请求类型验证
- 签名验证（有效/无效签名）
- 时间戳验证（过期检测）
- Nonce 去重验证

#### FeishuEventDecryptorTests
测试飞书事件解密服务：
- V1.0 版本事件解密
- V2.0 版本事件解密
- 无效密钥处理
- 无效 Base64 数据处理
- 空数据处理
- 取消令牌支持

#### FeishuWebhookServiceTests
测试飞书 Webhook 核心服务：
- 事件订阅验证
- 事件数据处理
- 重复事件去重（幂等性）
- 事件解密
- 签名验证
- 并发控制
- 超时处理

#### CircuitBreakerServiceTests
测试断路器服务：
- 成功执行返回结果
- 失败次数超过阈值打开断路器
- 断路器开启时拒绝请求
- 半开状态允许请求
- 半开状态成功后关闭断路器
- 半开状态失败后重新打开
- 手动重置断路器
- 手动触发断路器

### 2. 配置测试 (Configuration)

#### FeishuWebhookOptionsTests
测试 Webhook 配置选项：
- 默认值验证
- 自定义值设置
- 验证 Token 格式
- 加密密钥长度
- 超时配置
- 并发事件数配置

#### CircuitBreakerOptionsTests
测试断路器配置选项：
- 默认值验证
- 自定义值设置
- 异常阈值配置
- 断开时长配置
- 成功阈值配置

## 测试技术栈

- **测试框架**: xUnit 2.9.2
- **Mock 框架**: Moq 4.20.72
- **测试运行器**: Microsoft.NET.Test.Sdk 17.11.1
- **代码覆盖**: coverlet.collector 6.0.2

## 运行测试

### 使用 .NET CLI
```bash
# 运行所有测试
dotnet test Tests/Mud.Feishu.Webhook.Tests/Mud.Feishu.Webhook.Tests.csproj

# 运行测试并生成代码覆盖率报告
dotnet test Tests/Mud.Feishu.Webhook.Tests/Mud.Feishu.Webhook.Tests.csproj --collect:"XPlat Code Coverage"

# 运行特定测试类
dotnet test Tests/Mud.Feishu.Webhook.Tests/Mud.Feishu.Webhook.Tests.csproj --filter "FullyQualifiedName~CircuitBreakerServiceTests"
```

### 使用 Visual Studio
1. 打开测试资源管理器（Test Explorer）
2. 点击"运行所有测试"按钮
3. 查看测试结果和覆盖率

## 测试结构

```
Tests/Mud.Feishu.Webhook.Tests/
├── Configuration/
│   ├── FeishuWebhookOptionsTests.cs      # Webhook 配置测试
│   └── CircuitBreakerOptionsTests.cs     # 断路器配置测试
├── Services/
│   ├── FeishuEventValidatorTests.cs      # 事件验证测试
│   ├── FeishuEventDecryptorTests.cs      # 事件解密测试
│   ├── FeishuWebhookServiceTests.cs      # Webhook 服务测试
│   └── CircuitBreakerServiceTests.cs     # 断路器服务测试
├── GlobalUsings.cs                       # 全局引用
├── Mud.Feishu.Webhook.Tests.csproj      # 项目文件
└── README.md                             # 本文档
```

## 测试原则

1. **单元测试隔离**: 使用 Mock 对象隔离外部依赖
2. **AAA 模式**: 所有测试遵循 Arrange-Act-Assert 模式
3. **命名规范**: 测试方法命名格式为 `MethodName_Scenario_ExpectedBehavior`
4. **完整覆盖**: 覆盖正常流程、异常流程和边界条件
5. **快速执行**: 单元测试执行速度快，适合 CI/CD 集成

## 核心功能测试

### 事件验证
- ✅ 订阅请求验证（Token 和类型）
- ✅ 签名验证（HMAC-SHA256）
- ✅ 时间戳验证（防重放攻击）
- ✅ Nonce 去重验证

### 事件解密
- ✅ AES-256-CBC 解密
- ✅ V1.0 和 V2.0 版本支持
- ✅ 错误处理和日志记录
- ✅ 取消令牌支持

### Webhook 服务
- ✅ 事件订阅验证流程
- ✅ 事件处理流程
- ✅ 幂等性保证（去重）
- ✅ 并发控制
- ✅ 超时处理
- ✅ 异常处理

### 断路器
- ✅ 故障检测和断路
- ✅ 半开状态恢复
- ✅ 手动控制
- ✅ 状态转换

## 注意事项

- 所有测试使用 Mock 对象，不需要真实的飞书服务器
- 测试覆盖了同步和异步方法
- 测试验证了异常处理和边界条件
- 加密测试使用模拟的 AES 加密算法

## 贡献指南

添加新测试时，请遵循以下规范：
1. 使用清晰的测试方法命名
2. 添加必要的注释说明测试目的
3. 确保测试独立且可重复执行
4. 验证正常和异常情况
5. 更新本 README 文档

## 许可证

本项目遵循 MIT 许可证。详见根目录的 LICENSE 文件。
