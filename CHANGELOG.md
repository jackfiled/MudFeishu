# Mud.Feishu 更新日志
## [2.0.2] - 2026-01-30

### ✨ Added
- **性能指标监控**: 添加完整的性能指标收集功能
  - 新增 MeterExtensions、FeishuMetrics 和 FeishuMetricsHelper 类
  - 在 TokenManager、HttpClientUtils 等关键组件中添加指标记录
- **WebSocket 和 Webhook 指标**: 为 WebSocket 和 Webhook 添加专项指标监控
  - WebSocket 连接数统计和认证/事件处理指标
  - Webhook 签名验证、事件解密和处理指标
  - 事件去重命中指标
  - 支持 WebSocketConnectionCountProvider 获取实时连接数
- **调试日志增强**: 增加响应内容调试日志和错误处理
  - 添加调试日志记录原始响应内容
  - 在 JSON 反序列化失败时提供更详细的错误信息
- **测试控制器**: 新增日志测试控制器和网络测试控制器

### 🔧 Changed
- **依赖更新**: 更新 Mud.HttpUtils 依赖至 1.5.2 版本
- **HTTP 客户端优化**: 
  - 增强异常处理和日志记录
  - 改进安全配置和连接池设置
- **认证优化**: 优化令牌获取逻辑并移除冗余指标记录
  - 改为直接从缓存获取令牌时记录指标
- **配置简化**: 移除断路器功能及相关代码
  - 移除断路器配置项和文档说明
  - 简化代码结构和 Webhook 服务
- **时间戳验证**: 优化时间戳验证逻辑，优先使用应用特定配置
- **Demo 项目**: 调整 Demo 项目的配置和依赖

### 🐛 Fixed
- **JobLevel 接口**: 将 JobLevel 接口的 name 参数改为可空类型
- **JobFamilies 接口**: 允许 GetJobFamilesListAsync 的 name 参数为 null

### 📚 Documentation
- 更新 README 文档说明指标使用方式
- 添加日志测试相关文档

### 📦 Build & Config
- 更新项目版本至 2.0.2

## [2.0.1] - 2026-01-28

### 🚨 BREAKING CHANGE
- **移除 FeishuOptions 类** - 完全移除旧的配置类，所有场景统一使用 `FeishuAppConfig`
- **多应用架构支持** - API 签名和配置方式发生变化，需要迁移
- **配置系统重构** - 重试机制和 Token 管理配置方式改变

### ✨ Added
- **多应用支持**: 支持配置和管理多个飞书应用
- **应用上下文切换**: 新增 `IFeishuAppContextSwitcher` 接口支持运行时切换应用
- **自动推断默认应用**: 三种自动推断规则简化配置
- **应用级 Token 缓存隔离**: 每个应用拥有独立的 Token 缓存
- **新配置参数**: 新增 `RetryDelayMs` 和 `TokenRefreshThreshold` 配置
- **文档**: 新增配置迁移指南和多应用配置文档

### 🔧 Changed
- **重构 HttpClient 配置**: Polly 重试策略使用配置参数
- **重构 Token 重试逻辑**: 使用配置的 `RetryDelayMs` 替代硬编码值
- **WebSocket 重试**: 使用配置的 `RetryDelayMs` 替代硬编码值
- **依赖更新**: 替换代码生成器为 `Mud.HttpUtils`

### 🐛 Fixed
- **修复 `RetryCount` 配置不一致问题**: 统一应用到 HttpClient 和 TokenManager
- **WebSocket 重试硬编码问题**: 使用配置参数替代硬编码值

### 📚 Documentation
- 新增配置迁移指南
- 更新示例配置文件
- 新增多应用配置文档

---
## [1.2.2] - 2026-01-19

### ✨ Added
- **考勤管理API**: 新增完整的考勤班次管理能力
- **审批功能**: 新增审批消息API和审批任务查询接口
- **演示项目**: 新增飞书OAuth登录演示项目

### 🐛 Fixed
- **解密失败处理**: 修复解密失败时空引用问题
- **令牌管理**: 修复用户令牌管理及状态清理问题
- **项目文件**: 修复PackageTags标签重复闭合问题
- **Webhook中间件**: 修复验证请求属性和缩进问题

### 🔧 Changed
- **模型重构**: 班次模型提取公共基类
- **认证服务**: 重构认证服务和接口命名
- **项目结构**: 移动项目文件到Sources文件夹
- **工具类**: 重组异常和HTTP客户端工具
- **Redis性能**: 使用SCAN替代KEYS命令
- **缓存管理**: 优化令牌缓存和格式化逻辑
- **代码清理**: 移除未使用变量和多余引用

### 📚 Documentation
- API文档更新和完善
- 项目文档链接和版本号更新
- 演示项目文档优化和重组

### 📦 Build & Config
- 版本更新至1.2.2
- 依赖管理优化
- Git配置安全加固

---

## [1.2.1] - 2026-01-16

### ✨ Added
- **配置验证**: 新增AppId、AppSecret、EncryptKey格式和长度验证
- **敏感信息保护**: 配置类添加敏感信息掩码功能
- **示例配置**: 新增完整的appsettings.example.json文件

### 🔒 Security
- **数据注解验证**: 添加Data Annotations属性验证必填字段
- **Redis配置验证**: 新增ServerAddress格式和连接参数验证

### 🧪 Tests
- **配置单元测试**: 全面覆盖AppId/AppSecret验证、敏感信息掩码等

### 📚 Documentation
- **XML文档**: 完善配置类参数说明和示例值

### ⚠️ BREAKING CHANGE
- 修改了 `FeishuAppConfig.Validate()` 方法验证规则
- 添加了 `FeishuWebhookOptions.EncryptKey` 长度验证（32字符）
- 添加了 `RedisOptions.Validate()` 方法验证连接参数
---

## [1.1.2] - 2026-01-10

### ✨ Added
- **Webhook/WebSocket**: 异步验证、重试功能和请求体签名验证
- **审批功能**: 第三方审批实例验证、同步和状态分页接口
- **配置验证**: WebSocket和Feishu配置选项验证

### 🐛 Fixed
- **WebSocket配置**: 修复配置相关问题

### 🔧 Changed
- **事件处理器**: 调整为异步处理器设计
- **Redis服务**: 简化服务注册方法
- **服务注册**: 统一方法命名规范

### 📚 Documentation
- **英文文档**: 重构文档结构和内容组织

### 📦 Dependencies
- 更新项目依赖包至1.1.2版本

---

## [1.1.1] - 2026-01-06

### ✨ Added
- **审批功能**: 新增多种审批相关接口和常量
- **任务管理**: 自定义字段管理和选项管理功能
- **API响应**: 统一为FeishuApiResult格式

### 🔧 Changed
- **接口设计**: 统一消息发送接口
- **服务注册**: 统一服务注册API

### 📚 Documentation
- **README**: 优化项目描述和功能说明
- **架构文档**: 移除过时的架构设计文档

---

## [1.1.0] - 2025-11-12

### ✨ Added
- **用户管理**: 完整的用户CRUD操作接口
- **用户组管理**: 用户组创建、更新、删除接口
- **部门管理**: 完整的部门管理功能
- **员工类型管理**: 员工类型相关接口

### 🔧 Changed
- **API结果模型**: 重构为FeishuApiResult命名
- **服务注册**: 优化服务注册代码结构

### 📚 Documentation
- **README**: 更新内容和结构
- **功能说明**: 新增项目功能说明和使用示例

---

## [1.0.9] - 2025-11-14

### ✨ Added
- **跨平台支持**: 支持.NET Standard 2.0
- **HTTP客户端**: 新增飞书HTTP客户端扩展方法

### 🐛 Fixed
- **HttpClient配置**: 修复配置和API端点URL格式问题

### 🔧 Changed
- **消息和事件API**: 重构实现以提高可维护性
- **文件下载**: 优化HTTP请求方法提高性能

---

## [1.0.7] - 2025-11-12

### ✨ Added
- **任务管理**: 任务评论、附件、活动订阅和成员管理接口
- **JsTicket API**: 新增JsTicket API接口支持前端开发

### 🔧 Changed
- **代码生成器**: 升级Mud.ServiceCodeGenerator版本
- **依赖管理**: 优化项目依赖配置

### 📚 Documentation
- **任务文档**: 更新任务成员信息注释

---

## [1.0.3-dev] - 2025-11-12

### ✨ Added
- **基础框架**: 飞书API基础框架搭建
- **认证服务**: 认证服务和令牌管理
- **通讯录**: 企业通讯录相关接口
- **消息功能**: 消息发送和接收功能
- **事件支持**: Webhook和WebSocket支持
- **演示项目**: Webhook和WebSocket演示项目

### 📚 Documentation
- **项目文档**: 初始README和项目结构说明
