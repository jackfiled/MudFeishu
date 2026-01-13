# Mud.Feishu v1.2.0 - Bug 修复版本

**发布日期**: 2026-01-13 (计划中)  
**类型**: Bug 修复和安全增强

---

## 🔒 安全修复

### 高危问题修复

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

### 中危问题修复

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

---

## 🐛 Bug 修复

### 核心功能修复

- **后台处理模式失败事件存储**
  - 修复后台处理模式下失败事件无法被持久化的问题
  - 正确调用 `IFailedEventStore.StoreFailedEventAsync`
  - 添加详细的错误日志记录

### 资源管理修复

- **AES 加密资源释放**
  - 使用 `using var` 语句确保 AES 实例正确释放
  - 同时释放 ICryptoTransform 资源

- **SHA256 哈希资源释放**
  - 使用 `using` 语句确保 SHA256 实例正确释放

### 线程安全修复

- **失败事件存储清理方法**
  - 添加锁保护清理操作
  - 确保与字典操作并发安全

### 逻辑修复

- **SeqID 去重清理**
  - 统一在清理完成后重新计算最大 SeqID
  - 避免部分清理导致的计算错误

---

## 📝 文档改进

### 新增文档

- **SECURITY-WARNING.md**
  - Demo 项目安全配置指南
  - 生产环境安全检查清单
  - 环境变量配置说明

### 改进文档

- **README.md**
  - 更新安全配置建议
  - 添加最佳实践链接

---

## 🔄 破坏性变更

### 配置变更

- **生产环境签名验证**
  - 之前: 生产环境签名验证可以被禁用
  - 现在: 生产环境强制启用签名验证，禁用将抛出异常

- **Demo 项目配置**
  - 之前: appsettings.json 包含硬编码的示例密钥
  - 现在: 配置文件为空，必须通过环境变量或手动配置

---

## ⚠️ 迁移指南

### 生产环境配置

如果您在生产环境使用了以下配置，需要进行相应的调整：

#### 1. 启用签名验证

```json
{
  "FeishuWebhook": {
    "EnforceHeaderSignatureValidation": true,
    "EnableBodySignatureValidation": true
  }
}
```

#### 2. 配置环境变量

```bash
# Linux/macOS
export FeishuWebhook__VerificationToken="your_verification_token"
export FeishuWebhook__EncryptKey="your_32_byte_encryption_key"

# Windows PowerShell
$env:FeishuWebhook__VerificationToken="your_verification_token"
$env:FeishuWebhook__EncryptKey="your_32_byte_encryption_key"
```

#### 3. 验证配置

部署前请确认：
- [ ] 签名验证已启用
- [ ] 时间戳容错范围 <= 60 秒
- [ ] EncryptKey 为 32 字节强密钥
- [ ] IP 验证已启用并配置白名单
- [ ] 日志不包含敏感信息

---

## 📦 依赖更新

无依赖更新。

---

## 🧪 测试

### 新增测试

- 后台处理失败事件存储测试
- AES/SHA256 资源释放测试
- InMemoryFailedEventStore 线程安全测试
- SeqID 去重清理逻辑测试

---

## 🙏 贡献者

感谢以下贡献者对本版本的贡献：
- Mud.Feishu Team

---

## 📚 相关资源

- [Bug 修复计划](docs/bug-fix-plan.md)
- [代码审查报告](docs/code-review-report.md)
- [安全配置指南](docs/security.md)
- [部署指南](docs/deployment.md)

---

**下次发布**: v1.3.0 (计划日期: 2026-03-13)
