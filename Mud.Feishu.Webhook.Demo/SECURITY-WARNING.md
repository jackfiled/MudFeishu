# 🚨 安全警告 - Demo 项目配置

**重要**：本项目仅用于演示和测试目的，**切勿直接用于生产环境**。

---

## ⚠️ 配置要求

在运行此 Demo 项目之前，必须配置以下环境变量或更新 `appsettings.json`：

### 必需配置

1. **VerificationToken** - 飞书事件验证令牌
   ```bash
   # 通过环境变量设置（推荐）
   export FeishuWebhook__VerificationToken="your_verification_token_from_feishu"

   # 或在 appsettings.json 中设置（仅用于本地测试）
   "FeishuWebhook": {
     "VerificationToken": "your_actual_verification_token"
   }
   ```

2. **EncryptKey** - 事件加密密钥（必须是 32 字节）
   ```bash
   # 通过环境变量设置（推荐）
   export FeishuWebhook__EncryptKey="your_32_byte_encryption_key_123456"

   # 或在 appsettings.json 中设置（仅用于本地测试）
   "FeishuWebhook": {
     "EncryptKey": "your_32_byte_encryption_key_123456"
   }
   ```

---

## 🔒 生产环境配置差异

### 当前 Demo 配置（不安全）

```json
{
  "FeishuWebhook": {
    "VerificationToken": "",        // ❌ 未配置
    "EncryptKey": "",                 // ❌ 未配置
    "EnforceHeaderSignatureValidation": false,  // ❌ 签名验证禁用
    "EnableBodySignatureValidation": false,      // ❌ 请求体验证禁用
    "ValidateSourceIP": false         // ❌ IP 验证禁用
  }
}
```

### 生产环境配置（安全）

```json
{
  "FeishuWebhook": {
    "VerificationToken": "",        // ✅ 配置环境变量
    "EncryptKey": "",                 // ✅ 配置环境变量
    "EnforceHeaderSignatureValidation": true,   // ✅ 签名验证启用
    "EnableBodySignatureValidation": true,     // ✅ 请求体验证启用
    "ValidateSourceIP": true,         // ✅ IP 验证启用
    "AllowedSourceIPs": ["1.2.3.4"]  // ✅ 配置飞书官方 IP
  }
}
```

---

## 📋 安全检查清单

在部署到生产环境前，请确认：

- [ ] 所有敏感配置通过环境变量或密钥管理服务设置
- [ ] `EnforceHeaderSignatureValidation` 设置为 `true`
- [ ] `EnableBodySignatureValidation` 设置为 `true`
- [ ] `ValidateSourceIP` 设置为 `true`
- [ ] 配置飞书官方 IP 地址到 `AllowedSourceIPs`
- [ ] 设置合理的时间戳容错范围（建议 60 秒或更短）
- [ ] 使用强密钥（至少 32 字节，包含大小写字母、数字和特殊字符）
- [ ] 启用日志记录但避免记录敏感信息
- [ ] 配置监控和告警
- [ ] 定期更新依赖包

---

## 🔧 本地测试配置

仅用于本地开发和测试的安全配置：

```bash
# 设置环境变量
export ASPNETCORE_ENVIRONMENT="Development"
export FeishuWebhook__VerificationToken="your_test_token_123456"
export FeishuWebhook__EncryptKey="your_32_byte_test_enc_key_1234"

# 运行项目
dotnet run --project Mud.Feishu.Webhook.Demo.csproj
```

---

## 📚 相关资源

- [飞书开放平台 - 安全配置](https://open.feishu.cn/document/common-capabilities/message/event-subscription-guide/event-subscription-introduction)
- [Mud.Feishu 文档](../../docs/)
- [生产环境部署指南](../../docs/deployment.md)
- [安全最佳实践](../../docs/security.md)

---

## ⚡ 快速开始

1. 从飞书开放平台获取 VerificationToken 和 EncryptKey
2. 配置环境变量或更新 `appsettings.json`
3. 运行项目：`dotnet run`
4. 配置飞书事件回调地址：`http://your-server:80/feishu/Webhook`

---

**最后更新**: 2026-01-13
