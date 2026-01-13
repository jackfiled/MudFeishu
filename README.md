# MudFeishu

企业级 .NET 飞书 API 集成 SDK，提供完整的 HTTP API、WebSocket 实时事件订阅和 Webhook 事件处理解决方案，支持策略模式、工厂模式和自动令牌管理，适用于企业级应用开发。

## 📦 项目概览

| 组件 | 描述 | NuGet |
|-----|------|-------|
| **Mud.Feishu.Abstractions** | 事件订阅抽象层，提供策略模式和工厂模式的事件处理架构 | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Abstractions.svg)](https://www.nuget.org/packages/Mud.Feishu.Abstractions/) |
| **Mud.Feishu** | 核心 HTTP API 客户端库，支持组织架构、消息、群聊等完整飞书功能 | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.svg)](https://www.nuget.org/packages/Mud.Feishu/) |
| **Mud.Feishu.WebSocket** | 飞书 WebSocket 客户端，支持实时事件订阅和自动重连 | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.WebSocket.svg)](https://www.nuget.org/packages/Mud.Feishu.WebSocket/) |
| **Mud.Feishu.Webhook** | 飞书 Webhook 事件处理组件，支持 HTTP 回调事件接收和处理 | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Webhook.svg)](https://www.nuget.org/packages/Mud.Feishu.Webhook/) |
| **Mud.Feishu.Redis** | Redis 分布式去重扩展，支持多实例部署场景的事件去重 | [![Nuget](https://img.shields.io/nuget/v/Mud.Feishu.Redis.svg)](https://www.nuget.org/packages/Mud.Feishu.Redis/) |

## 🚀 快速开始

### 安装

```bash
# 事件处理抽象层
dotnet add package Mud.Feishu.Abstractions

# HTTP API 客户端
dotnet add package Mud.Feishu

# WebSocket 实时事件订阅
dotnet add package Mud.Feishu.WebSocket

# Webhook HTTP 回调事件处理
dotnet add package Mud.Feishu.Webhook

# Redis 分布式去重扩展
dotnet add package Mud.Feishu.Redis
```

### 基础配置

```csharp
using Mud.Feishu;
using Mud.Feishu.WebSocket;
using Mud.Feishu.Webhook;

var builder = WebApplication.CreateBuilder(args);

// 注册 HTTP API 服务（方式一：懒人模式 - 注册所有服务）
builder.Services.AddFeishuServices(builder.Configuration);

// 注册 HTTP API 服务（方式二：构造者模式 - 按需注册）
builder.Services.CreateFeishuServicesBuilder(builder.Configuration)
    .AddOrganizationApi()
    .AddMessageApi()
    .AddChatGroupApi()
    .AddApprovalApi()
    .AddTaskApi()
    .AddCardApi()
    .Build();

// 注册 HTTP API 服务（方式三：按模块注册）
builder.Services.AddFeishuServices(builder.Configuration, new[] {
    FeishuModule.Organization,
    FeishuModule.Message,
    FeishuModule.ChatGroup,
    FeishuModule.Approval,
    FeishuModule.Authentication
});

// 注册 HTTP API 服务（方式四：仅令牌管理服务）
builder.Services.AddFeishuTokenManagers(builder.Configuration);

// 注册 HTTP API 服务（方式五：代码配置）
builder.Services.CreateFeishuServicesBuilder(options =>
{
    options.AppId = "your_app_id";
    options.AppSecret = "your_app_secret";
    options.BaseUrl = "https://open.feishu.cn";
})
.AddAllApis()
.Build();

// 注册 WebSocket 事件订阅服务
builder.Services.AddFeishuWebSocketServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

// 注册 Webhook HTTP 回调事件服务
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .AddHandler<DepartmentCreatedEventHandler>()
    .Build();

var app = builder.Build();

// 添加 Webhook 中间件
app.UseFeishuWebhook();

app.Run();
```

### 配置文件

```json
{
    "Feishu": {
        "AppId": "demo_app_id",
        "AppSecret": "demo_app_secret",
        "BaseUrl": "https://open.feishu.cn",
        "TimeOut": 30,
        "RetryCount": 3,
        "EnableLogging": true,
        "WebSocket": {
            "AutoReconnect": true,
            "MaxReconnectAttempts": 5,
            "ReconnectDelayMs": 5000,
            "MaxReconnectDelayMs": 30000,
            "HeartbeatIntervalMs": 30000,
            "ConnectionTimeoutMs": 10000,
            "ReceiveBufferSize": 4096,
            "MaxMessageSize": 1048576,
            "EnableLogging": true,
            "EnableMessageQueue": true,
            "MessageQueueCapacity": 1000,
            "EmptyQueueCheckIntervalMs": 100,
            "MaxBinaryMessageSize": 10485760,
            "HealthCheckIntervalMs": 60000,
            "ParallelMultiHandlers": true
        },
        "Webhook": {
            "VerificationToken": "your_verification_token",
            "EncryptKey": "your_encrypt_key_32_bytes_long",
            "RoutePrefix": "feishu/Webhook",
            "AutoRegisterEndpoint": true,
            "EnableRequestLogging": true,
            "EnableExceptionHandling": true,
            "EnforceHeaderSignatureValidation": true,
            "EnableBodySignatureValidation": true,
            "TimestampToleranceSeconds": 60,
            "EventHandlingTimeoutMs": 30000,
            "MaxConcurrentEvents": 10,
            "EnablePerformanceMonitoring": false,
            "EnableBackgroundProcessing": false,
            "AllowedHttpMethods": ["POST"],
            "MaxRequestBodySize": 10485760,
            "ValidateSourceIP": false,
            "AllowedSourceIPs": [],
            "MultiAppEncryptKeys": {
                "cli_a1b2c3d4e5f6g7h8": "your_app1_encrypt_key_32_bytes_long",
                "cli_h8g7f6e5d4c3b2a1": "your_app2_encrypt_key_32_bytes_long"
            },
            "DefaultAppId": "cli_a1b2c3d4e5f6g7h8",
            "RateLimit": {
                "EnableRateLimit": false,
                "WindowSizeSeconds": 60,
                "MaxRequestsPerWindow": 100,
                "EnableIpRateLimit": true,
                "TooManyRequestsStatusCode": 429,
                "TooManyRequestsMessage": "请求过于频繁，请稍后再试",
                "WhitelistIPs": ["127.0.0.1", "::1"]
            }
        }
    }
}
```

## 📊 主要功能

### 🏛️ Mud.Feishu.Abstractions - 事件处理抽象层

#### 🎯 事件处理架构

| 功能特性 | 说明 | 事件类型 |
|----------|------|----------|
| **策略模式** | 可扩展的事件处理器架构，支持多种事件类型处理 | - |
| **工厂模式** | 内置事件处理器工厂，支持动态注册和发现 | - |
| **抽象基类** | 提供 `DefaultFeishuEventHandler<T>` 等基类简化开发 | - |
| **类型安全** | 强类型事件数据模型，编译时类型检查 | - |
| **异步处理** | 完全异步的事件处理，支持并行执行 | - |
| **可扩展性** | 易于扩展新的事件类型和处理器 | - |
| **组织管理事件** | 用户变更事件、部门组织架构变化 | 用户创建/更新/删除、部门变更 |
| **消息事件** | 接收新消息、发送状态通知、阅读状态变更 | 消息接收、发送状态、阅读状态 |
| **应用事件** | 应用授权相关事件、权限级别调整事件 | 应用授权、权限变更 |
| **自定义事件** | 支持企业自定义事件类型 | 企业自定义 |

### 🌐 Mud.Feishu - HTTP API 客户端功能

| 模块分类 | 主要功能 | API版本 | 说明 |
|---------|---------|---------|------|
| **🔐 认证与令牌管理** | 多类型令牌支持 | - | 支持应用令牌、租户令牌、用户令牌三种类型 |
|  | 自动令牌缓存 | - | 内置令牌缓存机制，减少API调用次数 |
|  | 智能令牌刷新 | - | 令牌即将过期时自动刷新，确保服务连续性 |
|  | 多租户支持 | - | 支持多租户场景下的令牌隔离和管理 |
|  | OAuth流程 | - | 完整的OAuth授权流程支持，安全获取用户令牌 |
| **🏢 组织架构管理** | 用户管理 | V1/V3 | 创建、更新、查询、删除、批量操作用户 |
|  | 部门管理 | V1/V3 | 部门树形结构维护，多层级部门管理 |
|  | 员工管理 | V1 | 员工档案和详细信息管理 |
|  | 职级管理 | - | 企业职级体系维护，职级增删改查 |
|  | 职位序列 | - | 职业发展路径管理，职位序列定义 |
|  | 角色权限 | - | 企业权限角色体系，角色成员管理 |
|  | 用户组管理 | - | 用户组成员管理，灵活的用户分组 |
|  | 工作城市管理 | - | 多城市工作地点维护 |
| **📱 消息服务** | 消息发送 | V1 | 支持文本、图片、文件、卡片等丰富类型消息 |
|  | 批量消息 | V1 | 向多用户/部门批量发送消息 |
|  | 群聊管理 | - | 群聊创建、成员管理、群聊信息维护 |
|  | 消息互动 | - | 消息表情回复、引用回复等互动功能 |
|  | 任务管理 | - | 任务创建、更新、状态管理等协作功能 |
| **🛠️ 企业级特性** | 统一异常处理 | - | 完善的异常处理机制，统一错误响应格式 |
|  | 智能重试机制 | - | 网络故障和临时错误的自动重试，提高稳定性 |
|  | 高性能缓存 | - | 解决缓存击穿和竞态条件，支持令牌自动刷新 |
|  | 连接池管理 | - | HTTP连接池复用，提升API调用效率 |
|  | 异步编程支持 | - | 全面的async/await支持，非阻塞I/O操作 |
|  | 详细日志记录 | - | 结构化日志，便于监控和问题排查 |

> 💡 **提示**：以上仅为功能模块示例，未展示全部 API 接口。更多详情请参考 [Mud.Feishu 详细文档](./Mud.Feishu/README.md)

### 🔄 Mud.Feishu.WebSocket - 实时事件订阅功能

| 功能分类 | 主要功能 | 说明 |
|---------|---------|------|
| **🤖 事件处理架构** | 策略模式设计 | 可扩展的事件处理器架构，支持自定义业务逻辑 |
|  | 多处理器支持 | 可注册多个事件处理器并行处理不同类型事件 |
|  | 单处理器模式 | 适合单一功能的简单事件处理场景 |
|  | 自定义处理器 | 完全可扩展的业务定制，支持复杂场景 |
|  | 事件重放 | 支持事件的重放和恢复机制，确保数据一致性 |
| **🫀 连接管理** | WebSocket连接管理 | 持久连接维护和监控 |
|  | 自动重连机制 | 连接断开时自动重新连接，确保服务连续性 |
|  | 心跳监控 | 定期心跳检测，确保连接活跃状态 |
|  | 连接负载均衡 | 多连接实例的负载分发，提升处理能力 |
|  | 优雅关闭 | 支持连接的优雅关闭和资源清理 |
| **📈 监控与运维** | 连接状态监控 | 实时连接数量、状态监控 |
|  | 事件处理统计 | 事件接收数量、处理时间统计 |
|  | 性能指标收集 | 消息处理吞吐量、延迟监控 |
|  | 健康检查 | 服务健康状态实时检查 |
|  | 告警支持 | 异常情况自动告警通知 |
|  | 详细审计日志 | 完整的事件处理审计记录 |

### 🌐 Mud.Feishu.Webhook - HTTP 回调事件处理功能

| 功能分类 | 主要功能 | 说明 |
|---------|---------|------|
| **🔒 安全验证与解密** | 事件订阅验证 | 支持飞书 URL 验证流程 |
|  | 请求签名验证 | 验证飞书事件请求的签名真实性，防止伪造请求 |
|  | 时间戳验证 | 防止重放攻击的时间戳检查，可配置容错范围 |
|  | AES-256-CBC解密 | 内置解密功能，自动处理飞书加密事件 |
|  | 来源IP验证 | 可配置的IP白名单验证，增强安全性 |
|  | 多机器人支持 | 支持多个飞书机器人共享同一 Webhook 端点 |
| **🚀 事件处理架构** | 中间件模式 | 无缝集成到 ASP.NET Core 管道 |
|  | 自动事件路由 | 根据事件类型自动分发到对应处理器 |
|  | 策略模式设计 | 可扩展的事件处理器架构，支持自定义业务逻辑 |
|  | 基类处理器 | 提供类型安全的基类处理器，自动去重和类型转换 |
|  | 异步处理 | 完全异步的事件处理机制，支持取消令牌 |
|  | 后台处理模式 | 支持异步后台处理，避免飞书超时重试 |
|  | 并发控制 | 可配置的并发事件处理数量限制，支持热更新 |
| **🛡️ 高级安全特性** | 请求频率限制 | 内置滑动窗口限流中间件，防止恶意请求 |
|  | 威胁检测 | 自动检测和阻止可疑请求模式 |
|  | 安全审计 | 详细的安全事件审计日志 |
|  | 密钥强度验证 | 启动时验证加密密钥强度和安全性 |
| **📊 监控与运维** | 性能监控 | 可选的性能指标收集和监控 |
|  | 健康检查 | 内置健康检查端点，支持失败率阈值配置 |
|  | 异常处理 | 完善的异常处理和日志记录 |
|  | 请求日志 | 详细的请求处理日志记录 |
|  | 诊断端点 | Demo项目提供诊断端点查看处理器注册情况 |
| **🔧 配置与扩展** | 配置热更新 | 支持运行时配置变更，无需重启服务 |
|  | 灵活配置 | 支持配置文件、代码配置和建造者模式 |
|  | 依赖注入 | 完全集成 .NET 依赖注入容器 |
|  | 跨平台支持 | 支持 .NET Standard 2.0, .NET 6.0, .NET 8.0, .NET 10.0 |

### 💾 Mud.Feishu.Redis - 分布式去重扩展

| 功能分类 | 主要功能 | 说明 |
|---------|---------|------|
| **🔄 分布式去重** | EventId 去重 | 基于事件ID的分布式去重，防止重复处理同一事件 |
|  | Nonce 去重 | 防止重放攻击，确保请求的唯一性 |
|  | SeqID 去重 | WebSocket 二进制消息序列号去重 |
|  | 原子性操作 | 使用 Redis SETNX + EXPIRE 确保去重操作的原子性 |
|  | 自动过期 | Redis 自动清理过期数据，无需手动维护 |
| **🌐 分布式支持** | 多实例部署 | 适用于多实例分布式部署场景 |
|  | 环境隔离 | 支持通过键前缀实现多环境数据隔离 |
|  | 集群支持 | 支持 Redis 集群和哨兵模式 |
|  | TLS/SSL 加密 | 支持安全连接到 Redis |
| **⚙️ 灵活配置** | 可配置过期时间 | 支持自定义事件、Nonce、SeqID 的缓存过期时间 |
|  | 键前缀定制 | 可自定义 Redis 键前缀，方便多租户场景 |
|  | 连接超时配置 | 可配置连接超时和同步超时时间 |
|  | 健康检查 | 内置 Redis 连接健康检查 |
| **📊 监控与诊断** | 日志记录 | 详细的去重操作日志 |
|  | 缓存统计 | 支持查询已处理的事件数量 |
|  | 性能优化 | 高效的 Redis 操作，最小化网络开销 |


## 🎯 使用场景

### 🚀 快速上手示例

#### HTTP API 调用示例

```csharp
// 用户管理 Controller 示例
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IFeishuTenantV3User _userApi;
    private readonly IFeishuTenantV3Departments _deptApi;
    
    public UserController(
        IFeishuTenantV3User userApi,
        IFeishuTenantV3Departments deptApi)
    {
        _userApi = userApi;
        _deptApi = deptApi;
    }
    
    // 创建新用户
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await _userApi.CreateUserAsync(request);
        return result.Code == 0 ? Ok(result.Data) : BadRequest(result.Msg);
    }
    
    // 获取部门下的所有用户
    [HttpGet("departments/{departmentId}/users")]
    public async Task<IActionResult> GetDepartmentUsers(string departmentId)
    {
        var result = await _deptApi.GetUserByDepartmentIdAsync(departmentId);
        return Ok(result.Data);
    }
    
    // 批量获取用户信息
    [HttpPost("users/batch")]
    public async Task<IActionResult> GetUsersBatch([FromBody] string[] userIds)
    {
        var result = await _userApi.GetUserByIdsAsync(userIds);
        return Ok(result.Data);
    }
}
```

#### 消息发送示例

```csharp
public class NotificationService
{
    private readonly IFeishuTenantV1Message _messageApi;
    private readonly IFeishuTenantV1BatchMessage _batchMessageApi;
    
    public NotificationService(
        IFeishuTenantV1Message messageApi,
        IFeishuTenantV1BatchMessage batchMessageApi)
    {
        _messageApi = messageApi;
        _batchMessageApi = batchMessageApi;
    }
    
    // 发送文本消息给用户
    public async Task<string> SendTextMessageAsync(string userId, string content)
    {
        var textContent = new MessageTextContent { Text = content };
        var request = new SendMessageRequest
        {
            ReceiveId = userId,
            MsgType = "text",
            Content = JsonSerializer.Serialize(textContent)
        };

        var result = await _messageApi.SendMessageAsync(request, receive_id_type: "user_id");
        return result.Code == 0 ? result.Data?.MessageId : null;
    }

    // 批量发送系统通知
    public async Task<string> SendSystemNotificationAsync(string[] departmentIds, string title, string content)
    {
        var request = new BatchSenderTextMessageRequest
        {
            DeptIds = departmentIds,
            Content = new MessageTextContent
            {
                Text = $"📢 {title}\n\n{content}"
            }
        };

        var result = await _batchMessageApi.BatchSendTextMessageAsync(request);
        return result.Code == 0 ? result.Data?.MessageId : null;
    }
}
```

#### WebSocket 事件处理示例

```csharp
using Mud.Feishu.Abstractions;
using System.Text.Json;

/// <summary>
/// 用户事件处理器 - 实现 IFeishuEventHandler 接口
/// </summary>
public class DemoUserEventHandler : IFeishuEventHandler
{
    private readonly ILogger<DemoUserEventHandler> _logger;
    private readonly IUserSyncService _syncService;

    public DemoUserEventHandler(ILogger<DemoUserEventHandler> logger, IUserSyncService syncService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
    }

    public string SupportedEventType => "contact.user.created_v3";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        try
        {
            // 解析用户数据
            var userData = ParseUserData(eventData);

            // 记录事件到服务
            await _syncService.RecordUserEventAsync(userData, cancellationToken);

            // 模拟业务处理
            await ProcessUserEventAsync(userData, cancellationToken);

            _logger.LogInformation("用户创建事件处理完成: 用户ID {UserId}, 用户名 {UserName}",
                userData.UserId, userData.UserName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理用户创建事件失败: {EventId}", eventData.EventId);
            throw;
        }
    }

    private UserData ParseUserData(EventData eventData)
    {
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(eventData.Event?.ToString() ?? "{}");
        var userElement = jsonElement.GetProperty("user");

        return new UserData
        {
            UserId = userElement.GetProperty("user_id").GetString() ?? "",
            UserName = userElement.GetProperty("name").GetString() ?? "",
            Email = TryGetProperty(userElement, "email") ?? "",
            Department = TryGetProperty(userElement, "department") ?? "",
            Phone = TryGetProperty(userElement, "phone") ?? "",
            Avatar = TryGetProperty(userElement, "avatar") ?? "",
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow
        };
    }

    private async Task ProcessUserEventAsync(UserData userData, CancellationToken cancellationToken)
    {
        _logger.LogDebug("开始处理用户数据: {UserId}", userData.UserId);

        // 模拟异步业务操作
        await Task.Delay(100, cancellationToken);

        // 模拟用户数据处理：数据库存储、缓存更新、通知发送等
        if (string.IsNullOrWhiteSpace(userData.UserId))
        {
            throw new ArgumentException("用户ID不能为空");
        }

        // 模拟发送欢迎通知
        _logger.LogInformation("发送欢迎通知给用户: {UserName} ({Email})",
            userData.UserName, userData.Email);

        // 模拟更新统计信息
        _syncService.IncrementUserCount();

        await Task.CompletedTask;
    }

    private static string? TryGetProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
    }
}

/// <summary>
/// 部门事件处理器 - 继承 DepartmentCreatedEventHandler 基类
/// </summary>
public class DemoDepartmentEventHandler : DepartmentCreatedEventHandler
{
    private readonly IDepartmentSyncService _syncService;

    public DemoDepartmentEventHandler(ILogger<DemoDepartmentEventHandler> logger, IDepartmentSyncService syncService)
        : base(logger)
    {
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        ObjectEventResult<DepartmentCreatedResult>? departmentData,
        CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        _logger.LogInformation("开始处理部门创建事件: {EventId}", eventData.EventId);

        try
        {
            // 记录事件到服务
            await _syncService.RecordDepartmentEventAsync(departmentData.Object, cancellationToken);

            // 模拟业务处理
            await ProcessDepartmentEventAsync(departmentData.Object, cancellationToken);

            _logger.LogInformation("部门创建事件处理完成: 部门ID {DepartmentId}, 部门名 {DepartmentName}",
                departmentData.Object.DepartmentId, departmentData.Object.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理部门创建事件失败: {EventId}", eventData.EventId);
            throw;
        }
    }

    private async Task ProcessDepartmentEventAsync(DepartmentCreatedResult departmentData, CancellationToken cancellationToken)
    {
        _logger.LogDebug("开始处理部门数据: {DepartmentId}", departmentData.DepartmentId);

        // 模拟异步业务操作
        await Task.Delay(100, cancellationToken);

        // 模拟验证逻辑
        if (string.IsNullOrWhiteSpace(departmentData.DepartmentId))
        {
            throw new ArgumentException("部门ID不能为空");
        }

        // 模拟权限初始化
        _logger.LogInformation("初始化部门权限: {DepartmentName}", departmentData.Name);

        // 模拟通知部门主管
        if (!string.IsNullOrWhiteSpace(departmentData.LeaderUserId))
        {
            _logger.LogInformation("通知部门主管: {LeaderUserId}", departmentData.LeaderUserId);
        }

        // 模拟更新统计信息
        _syncService.IncrementDepartmentCount();

        // 模拟层级关系处理
        if (!string.IsNullOrWhiteSpace(departmentData.ParentDepartmentId))
        {
            _logger.LogInformation("建立层级关系: {DepartmentId} -> {ParentDepartmentId}",
                departmentData.DepartmentId, departmentData.ParentDepartmentId);
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// 部门删除事件处理器 - 继承 DepartmentDeleteEventHandler 基类
/// </summary>
public class DemoDepartmentDeleteEventHandler : DepartmentDeleteEventHandler
{
    public DemoDepartmentDeleteEventHandler(ILogger<DepartmentDeleteEventHandler> logger)
        : base(logger)
    {
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        DepartmentDeleteResult? eventEntity,
        CancellationToken cancellationToken = default)
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        if (eventEntity == null)
        {
            _logger.LogWarning("部门删除事件实体为空，跳过处理");
            return;
        }

        _logger.LogInformation("开始处理部门删除事件: EventId={EventId}, AppId={AppId}, TenantKey={TenantKey}",
            eventData.EventId, eventData.AppId, eventData.TenantKey);

        _logger.LogDebug("部门删除事件详情: {@EventEntity}", eventEntity);

        // 执行部门删除相关的业务逻辑
        // 例如：清理部门缓存、更新统计数据、通知相关人员等

        await Task.CompletedTask;
    }
}
```

#### Webhook 事件处理示例

Webhook 事件处理器与 WebSocket 事件处理器使用相同的 `IFeishuEventHandler` 接口，因此代码可以复用。

##### 方式一：实现 IFeishuEventHandler 接口

```csharp
using Mud.Feishu.Abstractions;
using System.Text.Json;

// 用户创建事件处理器 - Webhook 和 WebSocket 都可以使用
public class UserCreatedEventHandler : IFeishuEventHandler
{
    private readonly ILogger<UserCreatedEventHandler> _logger;
    private readonly IUserSyncService _syncService;

    public UserCreatedEventHandler(
        ILogger<UserCreatedEventHandler> logger,
        IUserSyncService syncService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
    }

    public string SupportedEventType => "contact.user.created_v3";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("收到用户创建事件: EventId={EventId}", eventData.EventId);
            
            // 解析用户事件数据
            var userEvent = JsonSerializer.Deserialize<UserCreatedEvent>(
                eventData.Event?.ToString() ?? "{}");

            _logger.LogInformation("新用户创建: {UserName} ({UserId})",
                userEvent.User.Name, userEvent.User.UserId);

            // 同步用户到本地数据库
            await _syncService.SyncUserToDatabaseAsync(userEvent.User, cancellationToken);

            // 发送欢迎消息
            await SendWelcomeMessageAsync(userEvent.User.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理用户创建事件失败");
            throw;
        }
    }

    private async Task SendWelcomeMessageAsync(string userId)
    {
        // 发送欢迎消息逻辑
        await Task.CompletedTask;
    }
}
```

##### 方式二：继承基类处理器（推荐）

使用基类处理器可以获得自动去重和类型安全：

```csharp
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.DataModels.Organization;
using Mud.Feishu.Abstractions.EventHandlers;
using Mud.Feishu.Abstractions.Services;

// 部门创建事件处理器 - 继承基类
public class DepartmentCreatedHandler : DepartmentCreatedEventHandler
{
    private readonly IDepartmentService _deptService;

    public DepartmentCreatedHandler(
        IFeishuEventDeduplicator deduplicator,
        ILogger<DepartmentCreatedHandler> logger,
        IDepartmentService deptService)
        : base(deduplicator, logger)
    {
        _deptService = deptService;
    }

    protected override async Task ProcessBusinessLogicAsync(
        EventData eventData,
        DepartmentCreatedResult? eventEntity,
        CancellationToken cancellationToken = default)
    {
        // eventEntity 已经是强类型的部门数据
        _logger.LogInformation("处理部门创建: {Name}", eventEntity.Name);
        
        // 同步到本地数据库
        await _deptService.SyncDepartmentAsync(eventEntity, cancellationToken);
        
        // 初始化部门权限
        await _deptService.InitializePermissionsAsync(eventEntity.DepartmentId);
    }
}
```

##### 消息接收事件处理器

```csharp
// 消息接收事件处理器
public class MessageReceiveEventHandler : IFeishuEventHandler
{
    private readonly ILogger<MessageReceiveEventHandler> _logger;
    private readonly IFeishuTenantV1Message _messageApi;

    public MessageReceiveEventHandler(
        ILogger<MessageReceiveEventHandler> logger,
        IFeishuTenantV1Message messageApi)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageApi = messageApi ?? throw new ArgumentNullException(nameof(messageApi));
    }

    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            var messageEvent = JsonSerializer.Deserialize<MessageReceiveEvent>(
                eventData.Event?.ToString() ?? "{}");

            _logger.LogInformation("收到消息 - 发送者: {SenderId}, 内容: {Content}",
                messageEvent.Sender.Id, messageEvent.Message.Content);

            // 智能回复逻辑
            if (messageEvent.Message.Content.Contains("帮助"))
            {
                await SendHelpMessageAsync(messageEvent.Sender.Id);
            }
            
            // 关键词检测和自动回复
            if (messageEvent.Message.Content.Contains("报销"))
            {
                await SendExpenseGuideAsync(messageEvent.Sender.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理消息接收事件失败");
            throw;
        }
    }
    
    private async Task SendHelpMessageAsync(string userId)
    {
        // 发送帮助信息
        await Task.CompletedTask;
    }
    
    private async Task SendExpenseGuideAsync(string userId)
    {
        // 发送报销指南
        await Task.CompletedTask;
    }
}
```

##### 服务注册

```csharp
using Mud.Feishu.Webhook.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 注册 Webhook 服务
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<UserCreatedEventHandler>()      // 用户创建事件
    .AddHandler<DepartmentCreatedHandler>()     // 部门创建事件
    .AddHandler<MessageReceiveEventHandler>()   // 消息接收事件
    .EnableHealthChecks()                       // 启用健康检查
    .EnableMetrics()                            // 启用性能监控
    .Build();

var app = builder.Build();

// 添加 Webhook 中间件（自动处理 /feishu/Webhook 路由）
app.UseFeishuWebhook();

// 添加健康检查端点
app.MapHealthChecks("/health");

app.Run();
```

## 📖 详细文档

- [Mud.Feishu.Abstractions 详细文档](./Mud.Feishu.Abstractions/README.md) - 事件处理抽象层使用指南
- [Mud.Feishu 详细文档](./Mud.Feishu/README.md) - HTTP API 完整使用指南
- [Mud.Feishu.WebSocket 详细文档](./Mud.Feishu.WebSocket/Readme.md) - WebSocket 实时事件订阅指南
- [Mud.Feishu.Webhook 详细文档](./Mud.Feishu.Webhook/README.md) - Webhook HTTP 回调事件处理指南
- [Mud.Feishu.Redis 详细文档](./Mud.Feishu.Redis/README.md) - Redis 分布式去重扩展指南

## 🛠️ 技术栈

#### 核心依赖
- **Mud.ServiceCodeGenerator v1.4.6** - HTTP 客户端代码生成器
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

## 📄 许可证

本项目遵循 [MIT 许可证](./LICENSE)，允许商业和非商业用途。

## 🔗 相关链接

### 📖 官方文档
- [飞书开放平台文档](https://open.feishu.cn/document/) - 飞书 API 官方文档和最佳实践
- [NuGet 包管理器](https://www.nuget.org/) - .NET 包管理官方平台

### 📦 NuGet 包
- [Mud.Feishu.Abstractions](https://www.nuget.org/packages/Mud.Feishu.Abstractions/) - 事件处理抽象层
- [Mud.Feishu](https://www.nuget.org/packages/Mud.Feishu/) - 核心 HTTP API 客户端库
- [Mud.Feishu.WebSocket](https://www.nuget.org/packages/Mud.Feishu.WebSocket/) - WebSocket 实时事件订阅库
- [Mud.Feishu.Webhook](https://www.nuget.org/packages/Mud.Feishu.Webhook/) - Webhook HTTP 回调事件处理库
- [Mud.Feishu.Redis](https://www.nuget.org/packages/Mud.Feishu.Redis/) - Redis 分布式去重扩展库

### 🛠️ 开发资源
- [项目仓库](https://gitee.com/mudtools/MudFeishu) - 源代码和开发文档
- [Mud.ServiceCodeGenerator](https://gitee.com/mudtools/mud-code-generator) - HTTP 客户端代码生成器
- [示例项目](./Mud.Feishu.Test) - 完整的使用示例和演示代码

### 🤝 社区支持
- [问题反馈](https://gitee.com/mudtools/MudFeishu/issues) - Bug 报告和功能请求
- [贡献指南](./CONTRIBUTING.md) - 如何参与项目贡献
- [更新日志](./CHANGELOG.md) - 版本更新记录和变更说明