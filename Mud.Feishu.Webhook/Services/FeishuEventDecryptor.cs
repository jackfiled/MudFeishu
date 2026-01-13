// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 飞书事件解密服务实现
/// </summary>
public class FeishuEventDecryptor : IFeishuEventDecryptor
{
    private readonly ILogger<FeishuEventDecryptor> _logger;
    private const int BlockSize = 16;
    /// <inheritdoc />
    public FeishuEventDecryptor(ILogger<FeishuEventDecryptor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EventData?> DecryptAsync(string encryptedData, string encryptKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("开始解密事件数据");

            // Base64 解码
            var encryptedBytes = Convert.FromBase64String(encryptedData);

            // 使用 AES-256-CBC 解密
            var decryptedJson = await DecryptAes256CbcAsync(encryptedBytes, encryptKey, cancellationToken);

            if (string.IsNullOrEmpty(decryptedJson))
            {
                _logger.LogError("事件数据解密失败");
                return null;
            }

            // 解析事件数据（支持 v1.0 和 v2.0 版本）
            EventData eventData;

            using (var jsonDoc = JsonDocument.Parse(decryptedJson!))
            {
                var root = jsonDoc.RootElement;

                // 检查是否为v2.0版本
                if (root.TryGetProperty("schema", out var schemaElement) &&
                    schemaElement.GetString() == "2.0")
                {
                    // v2.0版本解析
                    eventData = ParseV2Event(root);
                }
                else
                {
                    // v1.0版本：直接反序列化
                    eventData = JsonSerializer.Deserialize<EventData>(decryptedJson!, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new EventData();
                }
            }

            if (!string.IsNullOrEmpty(eventData.EventType))
            {
                _logger.LogInformation("事件数据解密成功，事件类型: {EventType}, 事件ID: {EventId}",
                    eventData.EventType, eventData.EventId);
            }

            return eventData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解密事件数据时发生错误");
            return null;
        }
    }

    /// <summary>
    /// 解析v2.0版本的事件
    /// </summary>
    private EventData ParseV2Event(JsonElement root)
    {
        var eventData = new EventData();

        // 解析header
        if (root.TryGetProperty("header", out var headerElement))
        {
            if (headerElement.TryGetProperty("event_id", out var eventIdElement))
                eventData.EventId = eventIdElement.GetString() ?? string.Empty;

            if (headerElement.TryGetProperty("event_type", out var eventTypeElement))
                eventData.EventType = eventTypeElement.GetString() ?? string.Empty;

            if (headerElement.TryGetProperty("create_time", out var createTimeElement))
            {
                if (createTimeElement.ValueKind == JsonValueKind.String &&
                    long.TryParse(createTimeElement.GetString(), out var createTimeLong))
                {
                    eventData.CreateTime = createTimeLong / 1000; // 转换为秒
                }
                else if (createTimeElement.TryGetInt64(out var createTimeInt))
                {
                    eventData.CreateTime = createTimeInt / 1000;
                }
            }

            if (headerElement.TryGetProperty("tenant_key", out var tenantKeyElement))
                eventData.TenantKey = tenantKeyElement.GetString() ?? string.Empty;

            if (headerElement.TryGetProperty("app_id", out var appIdElement))
                eventData.AppId = appIdElement.GetString() ?? string.Empty;
        }

        // 解析event
        if (root.TryGetProperty("event", out var eventElement))
        {
            // 将event对象转换为JsonElement供后续使用
            eventData.Event = eventElement;
        }

        return eventData;
    }

    private static byte[] SHA256Hash(string str)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(str);
        return sha256.ComputeHash(bytes);
    }

    /// <summary>
    /// 使用 AES-256-CBC 解密数据
    /// </summary>
    /// <param name="encryptedBytes">加密的字节数组</param>
    /// <param name="encryptKey">加密密钥</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解密后的字符串</returns>
    private async Task<string?> DecryptAes256CbcAsync(byte[] encryptedBytes, string encryptKey, CancellationToken cancellationToken = default)
    {
        // 使用 Task.Run 将 CPU 密集型操作放到线程池执行，避免阻塞请求线程
        return await Task.Run(() =>
        {
            try
            {
                // 检查取消令牌
                cancellationToken.ThrowIfCancellationRequested();

                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("开始解密，密钥长度: {KeyLength}, 加密数据长度: {DataLength}", encryptKey.Length, encryptedBytes.Length);

                var rijndaelManaged = Aes.Create();
                rijndaelManaged.Key = SHA256Hash(encryptKey);
                rijndaelManaged.Mode = CipherMode.CBC;
                rijndaelManaged.IV = [.. encryptedBytes.Take(BlockSize)];
                ICryptoTransform transform = rijndaelManaged.CreateDecryptor();
                var result = transform.TransformFinalBlock(encryptedBytes, BlockSize, encryptedBytes.Length - BlockSize);
                // 再次检查取消令牌
                cancellationToken.ThrowIfCancellationRequested();
                if (result == null)
                {
                    _logger.LogError("解密失败，结果为空");
                    return null;
                }
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("解密成功，结果长度: {ResultLength}", result?.Length ?? 0);
                return Encoding.UTF8.GetString(result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("AES 解密操作被取消");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AES 解密时发生错误");
                return null;
            }
        }, cancellationToken);
    }
}
