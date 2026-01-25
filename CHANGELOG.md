# Mud.Feishu 更新日志

## 2.0.0 (2026-01-24)

**类型**: 重大更新、配置重构、支飞书多应用、基础设施升级

### ⚠️ 重大变更

#### 多应用架构支持

- **全面支持飞书多应用**
  - 文件: `FeishuAppConfig.cs`, `FeishuAppManager.cs`, `FeishuMultiAppExtensions.cs`
  - 新增: `FeishuAppConfig` 配置类，支持配置多个飞书应用
  - 新增: `IFeishuAppContext` 接口，提供应用级别的上下文访问
  - 新增: `IFeishuAppContextSwitcher` 接口，支持运行时切换应用上下文
  - 新增: `IFeishuMultiAppService` 多应用服务接口，管理多个应用实例
  - 影响: 单个应用实例可以同时管理和操作多个飞书应用
  - 核心特性:
    - 每个应用拥有独立的配置（AppId、AppSecret、TokenRefreshThreshold 等）
    - 每个应用拥有独立的 Token 缓存和 Token 刷新策略
    - 支持默认应用自动推断机制
    - 支持运行时动态切换应用上下文
    - 完全隔离的多应用环境，互不干扰

- **应用级 Token 缓存隔离**
  - 文件: `MemoryTokenCache.cs`, `PrefixedTokenCache.cs`, `FeishuAppManager.cs`
  - 新增: 每个应用创建独立的 `MemoryTokenCache` 实例
  - 新增: `PrefixedTokenCache` 包装器，为缓存键添加应用前缀确保隔离
  - 实现: 在 `FeishuAppManager.CreateAppContext()` 中为每个应用创建独立缓存
  - 影响: 不同应用的 Token 完全隔离，避免相互干扰
  - 示例代码:
    ```csharp
    // 为每个应用创建独立的 TokenCache 实例
    var tokenCache = new TokenManager.MemoryTokenCache(logger, config.TokenRefreshThreshold);
    var prefixedCache = new TokenManager.PrefixedTokenCache(tokenCache, config.AppKey);
    ```

- **应用上下文切换机制**
  - 文件: `FeishuAppContextSwitcher.cs`
  - 新增: 使用 `AsyncLocal<IFeishuAppContext>` 实现应用上下文隔离
  - 新增: `UseAppContext()` 方法，支持在代码块中临时切换应用
  - 新增: `GetAppContext()` 方法，获取当前应用上下文
  - 影响: 支持在单个请求中操作多个飞书应用
  - 使用示例:

    ```csharp
    // 获取应用上下文切换器
    var switcher = serviceProvider.GetRequiredService<IFeishuAppContextSwitcher>();

    // 切换到指定应用
    await switcher.UseAppContext("app1", async () => {
        // 在此代码块中，所有操作都是针对 app1 应用
        var context = switcher.GetAppContext();
        await context.Auth.GetTenantAccessTokenAsync();
    });

    // 切换到另一个应用
    await switcher.UseAppContext("app2", async () => {
        // 在此代码块中，所有操作都是针对 app2 应用
        var context = switcher.GetAppContext();
        await context.Message.SendMessageAsync(...);
    });
    ```

- **默认应用自动推断**
  - 文件: `FeishuAppConfig.cs`, `FeishuMultiAppExtensions.cs`
  - 新增: 三种自动推断规则
    1. `AppKey == "default"` → 自动标记为默认应用
    2. 只配置一个应用 → 自动标记为默认应用
    3. 配置多个应用且未指定默认 → 第一个应用自动标记为默认应用
  - 影响: 减少用户配置负担，向后兼容单应用场景

- **移除 FeishuOptions 类**
  - 文件: `FeishuOptions.cs` (已删除)
  - 变更: 完全移除旧的配置类，所有场景统一使用 `FeishuAppConfig`
  - 影响: 多应用配置成为唯一支持的配置方式

#### 配置系统重构

- **新增 `RetryDelayMs` 配置参数**
  - 文件: `FeishuAppConfig.cs`
  - 默认值: 1000毫秒（1秒）
  - 范围: 100-60000毫秒
  - 影响: 统一 HTTP 请求和 Token 获取的重试延迟策略

- **修复 `RetryCount` 配置不一致问题**
  - 文件: `FeishuServiceCollectionExtensions.cs`, `TokenManagerWithCache.cs`
  - 变更: `RetryCount` 现在统一应用到 HttpClient 和 TokenManager
  - 影响: 用户配置的重试次数现在实际生效

- **实现 `IsDefault` 自动推断逻辑**
  - 文件: `FeishuAppConfig.cs`, `FeishuMultiAppExtensions.cs`
  - 变更:
    - `AppKey == "default"` → 自动设置 `IsDefault = true`
    - 只配置一个应用 → 自动设置 `IsDefault = true`
    - 配置多个应用且未指定默认 → 第一个自动设置 `IsDefault = true`
  - 影响: 减少用户配置负担

- **移除 `FeishuOptions` 类**
  - 文件: `FeishuOptions.cs` (已删除)
  - 变更: 完全移除旧的配置类,所有场景统一使用 `FeishuAppConfig`
  - 影响: 多应用配置成为唯一支持的配置方式

- **应用级 TokenRefreshThreshold 配置**
  - 文件: `FeishuAppConfig.cs`, `MemoryTokenCache.cs`, `FeishuAppManager.cs`
  - 变更:
    - `FeishuAppConfig.TokenRefreshThreshold` 属性(默认300秒,范围60-3600秒)
    - 每个应用拥有独立的令牌刷新阈值配置
    - 移除 `MemoryTokenCache` 的硬编码刷新阈值
  - 影响: 不同应用可以配置不同的令牌刷新策略

### 📚 文档更新

- **新增配置迁移指南**
  - 文件: `docs/Configuration-Migration-Guide.md`
  - 内容: 详细的配置变更说明和迁移步骤

- **更新示例配置文件**
  - 文件: `appsettings.example.json`, `Demos/**/*.appsettings.json`
  - 变更: 添加 `RetryDelayMs` 参数，移除冗余的 `IsDefault` 设置

- **新增多应用配置文档**
  - 文件: `Mud.Feishu/README.md`, `README.md`
  - 内容: 多应用配置说明、应用上下文切换示例、最佳实践

### 🔧 代码优化

#### 配置架构优化

- **重构 HttpClient 配置**
  - 文件: `FeishuServiceCollectionExtensions.cs`
  - 变更: Polly 重试策略使用配置参数
  - 影响: 支持自定义重试次数和延迟

- **重构 Token 重试逻辑**
  - 文件: `TokenManagerWithCache.cs`
  - 变更: 使用配置的 `RetryDelayMs` 替代硬编码值
  - 影响: 支持自定义 Token 获取重试延迟

- **修复 WebSocket 重试硬编码问题**
  - 文件: `FeishuWebSocketManager.cs`
  - 变更: 使用配置的 `RetryDelayMs` 替代硬编码的 1000ms
  - 影响: WebSocket 模块也支持自定义重试延迟

#### 依赖和注解优化

- **替换代码生成器为HttpUtils**
  - 文件: 全项目范围
  - 变更:
    - 替换 `Mud.ServiceCodeGenerator` 为 `Mud.HttpUtils`
    - 更新所有 `EventHandler` 注解为 `GenerateEventHandler`
    - 移除对 `Mud.CodeGenerator` 的依赖
    - 修改 `IFeishuAppContextSwitcher` 接口中的 `IgnoreGenerator` 注解为 `IgnoreImplement`
  - 影响: 提高代码的可维护性和可读性,统一HTTP客户端生成机制

### 🧪 测试更新

- **更新配置测试**
  - 文件: `FeishuAppConfigTests.cs`
  - 新增: 验证 `RetryDelayMs` 和 `TokenRefreshThreshold` 范围验证
  - 新增: 测试默认值和自动推断逻辑

- **更新 `IsDefault` 自动推断测试**
  - 文件: `MultiAppTests.cs`
  - 变更: 测试从验证异常改为验证自动推断逻辑
  - 新增: 测试 `RetryDelayMs` 配置

### 📖 使用示例

#### 单应用配置（自动默认）

**之前**:

```json
{
  "FeishuApps": [
    {
      "AppKey": "default",
      "AppId": "cli_xxx",
      "AppSecret": "dsk_xxx",
      "RetryCount": 3,
      "IsDefault": true
    }
  ]
}
```

**现在**:

```json
{
  "FeishuApps": [
    {
      "AppKey": "default",
      "AppId": "cli_xxx",
      "AppSecret": "dsk_xxx",
      "RetryCount": 3,
      "RetryDelayMs": 1000,
      "TokenRefreshThreshold": 300
    }
  ]
}
```

#### 多应用配置

**配置多个飞书应用**:

```json
{
  "FeishuApps": [
    {
      "AppKey": "app1",
      "AppId": "cli_aaa",
      "AppSecret": "dsk_aaa",
      "RetryCount": 3,
      "RetryDelayMs": 1000,
      "TokenRefreshThreshold": 300
    },
    {
      "AppKey": "app2",
      "AppId": "cli_bbb",
      "AppSecret": "dsk_bbb",
      "RetryCount": 5,
      "RetryDelayMs": 2000,
      "TokenRefreshThreshold": 600
    },
    {
      "AppKey": "app3",
      "AppId": "cli_ccc",
      "AppSecret": "dsk_ccc",
      "IsDefault": true
    }
  ]
}
```

#### 代码中使用多应用

```csharp
// 获取多应用服务
var multiAppService = serviceProvider.GetRequiredService<IFeishuMultiAppService>();

// 获取应用上下文切换器
var switcher = serviceProvider.GetRequiredService<IFeishuAppContextSwitcher>();

// 方式1: 使用默认应用
var defaultContext = multiAppService.GetDefaultAppContext();
await defaultContext.Message.SendMessageAsync(...);

// 方式2: 切换到指定应用上下文
await switcher.UseAppContext("app1", async () => {
    var context = switcher.GetAppContext();
    // 所有操作都针对 app1
    await context.Auth.GetTenantAccessTokenAsync();
    await context.Message.SendMessageAsync(...);
});

// 方式3: 直接获取指定应用上下文
var app2Context = multiAppService.GetAppContext("app2");
await app2Context.Message.SendMessageAsync(...);

// 方式4: 遍历所有应用
foreach (var appKey in multiAppService.GetAvailableAppKeys()) {
    var context = multiAppService.GetAppContext(appKey);
    // 对每个应用执行操作
    await context.Auth.GetTenantAccessTokenAsync();
}
```

### 🔄 迁移路径

#### 从单应用到多应用

如果之前使用的是单应用模式（`FeishuOptions`），请按以下步骤迁移:

**步骤1: 更新配置文件**

将原有的 `FeishuOptions` 配置改为 `FeishuApps` 数组:

```json
// 旧配置 (已废弃)
{
  "FeishuOptions": {
    "AppId": "cli_xxx",
    "AppSecret": "dsk_xxx",
    "RetryCount": 3
  }
}

// 新配置 (推荐)
{
  "FeishuApps": [
    {
      "AppKey": "default",
      "AppId": "cli_xxx",
      "AppSecret": "dsk_xxx",
      "RetryCount": 3,
      "RetryDelayMs": 1000,
      "TokenRefreshThreshold": 300
    }
  ]
}
```

**步骤2: 更新服务注册代码**

```csharp
// 旧注册方式 (已废弃)
services.AddFeishuApiService<FeishuOptions>(options => {
    builder.Configuration.Bind("FeishuOptions", options);
});

// 新注册方式 (推荐)
services.AddFeishuMultiAppServices(options => {
    builder.Configuration.Bind("FeishuApps", options);
});
```

**步骤3: 更新代码中的使用方式**

```csharp
// 旧使用方式 (已废弃)
var authService = serviceProvider.GetRequiredService<IFeishuV3Authentication>();
var token = await authService.GetTenantAccessTokenAsync();

// 新使用方式 - 默认应用 (推荐)
var multiAppService = serviceProvider.GetRequiredService<IFeishuMultiAppService>();
var defaultContext = multiAppService.GetDefaultAppContext();
var token = await defaultContext.Auth.GetTenantAccessTokenAsync();

// 新使用方式 - 指定应用
var appContext = multiAppService.GetAppContext("app1");
var token = await appContext.Auth.GetTenantAccessTokenAsync();

// 新使用方式 - 应用上下文切换
var switcher = serviceProvider.GetRequiredService<IFeishuAppContextSwitcher>();
await switcher.UseAppContext("app1", async () => {
    var context = switcher.GetAppContext();
    var token = await context.Auth.GetTenantAccessTokenAsync();
});
```

**步骤4: 添加新配置参数（可选）**

配置文件中添加 `RetryDelayMs` 和 `TokenRefreshThreshold` 参数（可选）

**步骤5: 移除 `IsDefault` 的显式设置**

系统会自动推断默认应用，无需显式设置 `IsDefault`

**重要提示**:

- `FeishuOptions` 类已被完全移除，必须迁移到 `FeishuAppConfig` 多应用模式
- 单个应用场景下，迁移后行为完全兼容
- 每个应用现在拥有独立的 Token 缓存和刷新策略

### 📖 参考文档

- [配置迁移指南](docs/Configuration-Migration-Guide.md)
- [多应用配置](https://github.com/mudtools/MudFeishu/wiki/Multi-App-Migration)

---

## 1.2.2 (2026-01-19)

**类型**: 功能增强、代码重构、文档完善、Bug修复

### 🚀 功能增强

#### 考勤管理API

- **新增考勤管理API服务支持**
  - 文件: 相关考勤模块
  - 影响: 提供完整的考勤班次管理能力

- **班次管理接口**
  - 新增: 按名称查询班次接口
  - 新增: 获取班次详情接口及相关数据模型
  - 新增: 删除班次接口并修正创建班次接口路径
  - 新增: 考勤班次相关数据模型和接口
  - 影响: 支持班次的创建、查询、删除等完整操作

#### 审批功能扩展

- **审批消息API**
  - 新增: 审批 Bot 消息更新接口及请求模型
  - 新增: 审批Bot消息相关数据模型和接口
  - 影响: 支持审批消息的实时更新和管理

- **审批任务查询**
  - 新增: 审批任务查询接口及相关数据模型
  - 影响: 提供更全面的审批任务管理能力

#### 演示项目增强

- **飞书OAuth登录演示**
  - 新增: 飞书OAuth登录演示项目
  - 影响: 提供完整的OAuth登录集成示例

### 🐛 Bug修复

- **解密失败处理**
  - 修复: 解密失败时处理验证请求的空引用问题
  - 影响: 提高解密异常处理的稳定性

- **令牌管理修复**
  - 修复: 用户令牌管理及状态清理问题
  - 影响: 确保令牌正确管理和释放

- **项目文件修复**
  - 修复: 项目文件中PackageTags标签的重复闭合问题
  - 影响: 解决项目文件格式问题

- **Webhook中间件修复**
  - 修复: 移除验证请求属性并修复中间件缩进
  - 影响: 优化Webhook中间件代码结构

### 🔧 代码重构

#### 模型和接口重构

- **班次模型重构**
  - 重构: 班次相关模型，提取公共基类
  - 影响: 提高代码复用性和可维护性

- **认证服务重构**
  - 重构: 认证服务和审批查询接口
  - 影响: 优化服务架构

- **命名空间和接口重命名**
  - 重构: 重命名 IFeishuV3AuthenticationApi 为 IFeishuV3Authentication
  - 影响: 统一接口命名规范

#### 项目结构优化

- **文件组织改善**
  - 重构: 将项目文件移动到Sources文件夹以改善组织结构
  - 影响: 优化项目目录结构

- **工具类重构**
  - 重构: 移动ExceptionUtils和HttpClientUtils到Utilities命名空间
  - 重构: 重构日志脱敏功能并集中到公共工具类
  - 影响: 统一工具类管理

#### 性能和安全优化

- **Redis命令优化**
  - 重构: 使用SCAN替代KEYS命令避免阻塞Redis服务
  - 影响: 提高Redis操作的性能和安全性

- **缓存管理优化**
  - 重构: 合并生成缓存键的方法并支持用户ID参数
  - 重构: 重构令牌管理器的缓存和格式化逻辑
  - 重构: 重构令牌管理模块，引入缓存抽象层
  - 影响: 优化缓存管理逻辑

- **代码清理**
  - 重构: 移除未使用的变量和多余的using声明
  - 重构: 移除独立的健康检查扩展类并简化注册逻辑
  - 重构: 重命名健康检查命名空间并更新引用
  - 影响: 提高代码质量和可维护性

### 📝 文档完善

- **API文档更新**
  - 完善: 审批消息数据模型的注释
  - 影响: 提高API文档的完整性

- **项目文档更新**
  - 更新: README中的项目克隆链接
  - 更新: 文档中的仓库链接和版本号
  - 新增: Mud.Feishu 演示项目集的 README 文档
  - 影响: 提供更准确的项目信息

- **演示文档优化**
  - 移除: WebSocket拦截器文档并更新Webhook文档
  - 更新: 飞书WebSocket演示项目的README文档
  - 移动: 飞书OAuth演示README文件位置
  - 影响: 优化演示文档结构

### 📦 构建和配置

- **版本更新**
  - 更新: 项目版本至1.2.2并同步依赖
  - 影响: 发布新版本

- **依赖管理优化**
  - 更新: 将HTTP相关依赖从全局移至特定项目
  - 影响: 优化依赖管理

- **Git配置更新**
  - 更新: .gitignore文件以排除敏感配置和发布目录
  - 影响: 提高版本控制的安全性

## 1.2.1 (2026-01-16)

**类型**: 配置增强、安全加固、代码质量提升

### 🔒 安全增强

#### 必填字段验证强化

- **FeishuAppConfig 配置验证**
  - 文件: `Mud.Feishu.Abstractions/Configuration/FeishuAppConfig.cs`
  - 新增: AppId 格式验证（必须以 `cli_` 或 `app_` 开头）
  - 新增: AppId 长度验证（至少 20 字符）
  - 新增: AppSecret 长度验证（至少 16 字符）
  - 新增: Data Annotations 属性（`[Required]`, `[RegularExpression]`, `[MinLength]`）
  - 影响: 启动时即可发现配置错误，避免运行时异常

- **FeishuWebhookOptions 配置验证**
  - 文件: `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
  - 新增: EncryptKey 长度验证（必须为 32 字符）
  - 新增: Data Annotations 属性到必填字段
  - 影响: 确保飞书事件加密密钥配置正确

#### 敏感信息保护

- **配置类敏感信息掩码**
  - 文件:
    - `Mud.Feishu.Abstractions/Configuration/FeishuAppConfig.cs`
    - `Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs`
    - `Mud.Feishu.Redis/Configuration/RedisOptions.cs`
  - 新增: `ToString()` 方法重写，自动掩码敏感信息
  - 实现: 显示首尾 2 个字符，中间用 `****` 替换
  - 影响: 防止日志泄露密钥等敏感信息

- **Redis 配置验证**
  - 文件: `Mud.Feishu.Redis/Configuration/RedisOptions.cs`
  - 新增: ServerAddress 格式验证
  - 新增: 连接超时参数范围验证
  - 新增: Data Annotations 属性
  - 影响: 确保 Redis 连接配置正确

### 🔧 配置优化

#### 配置示例文件

- **appsettings.example.json**
  - 文件: `appsettings.example.json` (新增)
  - 新增: 完整的配置示例文件
  - 包含: Feishu、FeishuWebhook、FeishuWebSocket、Redis 配置
  - 影响: 新用户可快速了解配置结构

### 🧪 测试覆盖

#### 配置单元测试

- **FeishuAppConfig 测试**
  - 文件: `Tests/Mud.Feishu.Abstractions.Tests/Configuration/FeishuAppConfigTests.cs`
  - 覆盖: AppId/AppSecret 验证、范围限制、敏感信息掩码、自动推断逻辑
  - 测试用例:
    - 有效配置验证
    - AppId 格式验证（cli*/app* 前缀）
    - AppId/AppSecret 空值验证
    - IsDefault 自动推断逻辑
    - TokenRefreshThreshold 配置验证
    - AppId/AppSecret 长度验证
    - TimeOut/RetryCount 范围限制
    - BaseUrl 格式验证
    - ToString 敏感信息掩码
  - 影响: 核心配置逻辑测试覆盖

### 📝 文档改进

#### XML 文档注释完善

- **配置类文档更新**
  - 文件: 所有 Options 配置类
  - 更新: 添加完整的参数说明和示例值
  - 新增: Data Annotations 错误信息说明
  - 影响: 提升代码可读性和 IDE 智能提示

### 🔨 Breaking Changes

- 修改了 `FeishuAppConfig.Validate()` 方法，现在会验证 AppId/AppSecret 格式
- 添加了 `FeishuWebhookOptions.EncryptKey` 长度验证（32 字符）
- 添加了 `RedisOptions.Validate()` 方法，验证连接参数

### 📦 依赖更新

- 新增依赖:
  - `System.ComponentModel.DataAnnotations` (用于 Data Annotations)

---

## 1.1.2 (2025-11-17)

**类型**: 功能增强、代码重构、Bug修复、文档完善

### 🚀 功能增强

- **API服务支持**
  - 新增: 考勤管理API服务支持
  - 新增: 班次管理相关接口
  - 新增: 审批消息API和审批任务查询接口
  - 影响: 提供更全面的飞书功能集成

- **项目结构优化**
  - 新增: 将项目文件移动到Sources文件夹
  - 影响: 改善项目组织结构

### 🐛 Bug修复

- **解密失败处理**
  - 修复: 解密失败时处理验证请求的空引用问题
  - 影响: 提高异常处理稳定性

- **用户令牌管理**
  - 修复: 用户令牌管理及状态清理问题
  - 影响: 确保令牌正确管理和释放

### 🔧 代码重构

- **认证服务重构**
  - 重构: 认证服务和审批查询接口
  - 重构: 重命名IFeishuV3AuthenticationApi为IFeishuV3Authentication
  - 影响: 统一接口命名规范

- **Redis命令优化**
  - 重构: 使用SCAN替代KEYS命令避免阻塞Redis服务
  - 影响: 提高Redis操作性能和安全性

- **缓存管理优化**
  - 重构: 合并生成缓存键的方法并支持用户ID参数
  - 重构: 重构令牌管理器的缓存和格式化逻辑
  - 影响: 优化缓存管理逻辑

### 📝 文档完善

- **API文档更新**
  - 完善: 审批消息数据模型的注释
  - 影响: 提高API文档完整性

- **项目文档更新**
  - 更新: README中的项目克隆链接
  - 更新: 文档中的仓库链接和版本号
  - 影响: 提供更准确的项目信息

## 1.1.2 (2026-01-10)

**类型**: 功能增强、代码重构、Bug修复、文档完善

### 🚀 功能增强

- **Webhook和WebSocket功能**
  - 新增: Webhook验证方式改为异步实现并重构重试服务
  - 新增: WebSocket事件重试功能，支持分布式和内存两种模式
  - 新增: 事件处理中状态同步和重连机制
  - 新增: 请求体签名验证选项
  - 影响: 提高事件处理的可靠性和稳定性

- **审批功能扩展**
  - 新增: 第三方审批实例验证功能
  - 新增: 第三方审批实例同步功能
  - 新增: 审批实例状态分页获取接口
  - 影响: 增强审批功能的完整性

- **配置验证功能**
  - 新增: WebSocket和Feishu配置选项
  - 新增: Webhook配置验证功能并优化选项文档
  - 影响: 提高配置的正确性和安全性

### 🐛 Bug修复

- **项目配置修复**
  - 修复: WebSocket配置相关问题
  - 影响: 确保WebSocket功能正常运行

### 🔧 代码重构

- **事件处理器改进**
  - 重构: 调整事件处理器基类设计以支持异步处理
  - 重构: 更新事件处理器基类为异步处理器
  - 影响: 提高事件处理的效率

- **Redis服务注册**
  - 重构: 简化Redis服务注册方法并更新文档
  - 影响: 简化Redis服务的使用

- **服务注册重构**
  - 重构: 重命名FeishuWebhook服务注册方法名
  - 重构: 重构飞书服务注册方法命名
  - 影响: 统一服务注册方法命名规范

### 📝 文档完善

- **英文文档重构**
  - 更新: 重新组织英文文档结构并恢复功能特性说明
  - 影响: 提高英文文档的可读性

### 📦 依赖更新

- 更新: 项目依赖包版本至1.1.2
- 更新: Mud.ServiceCodeGenerator版本
- 影响: 确保依赖包的兼容性和稳定性

## 1.1.1 (2026-01-06)

**类型**: 功能增强、代码重构、文档完善

### 🚀 功能增强

- **审批功能扩展**
  - 新增: 审批事件类型常量（WorkApproval和WorkApprovalRevert）
  - 新增: 通过审批定义Code获取详细数据的接口
  - 新增: 第三方审批定义创建功能
  - 新增: 审批实例评论分页查询接口
  - 新增: RemoveCommentsAsync接口用于清空审批实例下的所有评论与审批回复
  - 新增: 审批评论删除功能并优化创建评论参数
  - 新增: 审批任务签字功能
  - 新增: 审批任务通过审批定义Code获取详细数据的接口
  - 新增: 审批实例状态分页查询接口
  - 影响: 提供更完整的审批流程管理能力

- **任务管理功能**
  - 新增: 自定义字段管理接口
  - 新增: 自定义字段选项管理
  - 新增: 自定义字段资源绑定
  - 新增: 自定义字段列表分页查询
  - 影响: 支持任务的自定义字段管理

### 🔧 代码重构

- **API响应类型统一**
  - 重构: 更新所有API响应类型为FeishuApiResult系列
  - 影响: 统一API响应格式

- **消息发送接口重构**
  - 重构: 统一消息发送接口设计
  - 影响: 提高消息发送功能的一致性

- **服务注册API统一**
  - 重构: 统一服务注册API并移除冗余的UseMultiHandler方法
  - 影响: 简化服务注册流程

### 📝 文档完善

- **README文档优化**
  - 更新: 移除架构设计和性能特性文档
  - 更新: 添加部门事件处理器文档和使用示例
  - 更新: 优化项目描述和功能说明
  - 影响: 提高文档的实用性和可读性

## 1.1.0 (2025-11-12)

**类型**: 功能增强、代码重构、文档完善

### 🚀 功能增强

- **用户管理API**
  - 新增: 创建用户、更新用户、删除用户等完整用户管理接口
  - 新增: 批量查询用户信息接口
  - 新增: 恢复已删除用户接口
  - 影响: 提供全面的用户管理能力

- **用户组管理API**
  - 新增: 用户组创建、更新、删除接口
  - 新增: 查询用户所属组和用户组详情接口
  - 影响: 支持完整的用户组管理

- **部门管理API**
  - 新增: 创建部门、更新部门、删除部门接口
  - 新增: 根据父部门ID查询部门、根据部门ID查询父部门接口
  - 影响: 提供完整的部门管理功能

- **员工类型管理API**
  - 新增: 员工类型相关接口
  - 影响: 支持员工类型管理

### 🔧 代码重构

- **API结果模型重构**
  - 重构: ApiListResult列表数据结果集合
  - 重构: 将ApiResult重命名为FeishuApiResult
  - 影响: 统一API结果模型命名

- **服务注册优化**
  - 重构: 服务注册代码结构
  - 影响: 提高服务注册效率

### 📝 文档完善

- **README文档优化**
  - 更新: README文件内容和结构
  - 新增: 项目功能说明和使用示例
  - 影响: 提高文档可读性

## 1.0.9 (2025-11-14)

**类型**: 功能增强、代码重构、Bug修复

### 🚀 功能增强

- **跨平台支持**
  - 新增: 支持.NET Standard 2.0
  - 影响: 提高框架兼容性

- **HTTP客户端增强**
  - 新增: 飞书HTTP客户端扩展方法
  - 新增: 增强型HTTP客户端配置
  - 影响: 提供更灵活的HTTP请求配置

### 🐛 Bug修复

- **HttpClient配置修复**
  - 修复: HttpClient配置和API端点URL格式问题
  - 影响: 确保API请求正确发送

### 🔧 代码重构

- **消息和事件API重构**
  - 重构: 消息和事件API实现
  - 影响: 提高代码可维护性

- **文件下载功能重构**
  - 重构: 文件下载功能并优化HTTP请求方法
  - 影响: 提高文件下载性能

## 1.0.7 (2025-11-12)

**类型**: 功能增强、代码重构、文档完善

### 🚀 功能增强

- **任务管理API**
  - 新增: 任务评论相关接口
  - 新增: 任务附件管理接口
  - 新增: 任务活动订阅接口
  - 新增: 任务列表成员管理接口
  - 影响: 提供完整的任务管理能力

- **JsTicket API**
  - 新增: JsTicket API接口
  - 影响: 支持前端JavaScript开发需求

### 🔧 代码重构

- **代码生成器升级**
  - 升级: Mud.ServiceCodeGenerator版本
  - 影响: 提高代码生成效率和质量

- **项目依赖更新**
  - 更新: 项目依赖配置并移除测试工具
  - 影响: 优化项目依赖管理

### 📝 文档完善

- **任务相关文档更新**
  - 更新: 任务成员信息注释
  - 影响: 提高API文档准确性

## 1.0.3-dev (2025-11-12)

**类型**: 初始化版本、功能增强

### 🚀 功能增强

- **核心功能实现**
  - 新增: 飞书API基础框架搭建
  - 新增: 认证服务和令牌管理
  - 新增: 企业通讯录相关接口
  - 新增: 消息发送和接收功能
  - 新增: Webhook和WebSocket支持
  - 影响: 建立项目基础架构

- **演示项目**
  - 新增: Webhook演示服务
  - 新增: WebSocket演示项目
  - 影响: 提供使用示例

### 📝 文档完善

- **项目文档初始化**
  - 新增: README.md文件
  - 新增: 项目结构说明
  - 影响: 提供项目基础文档
