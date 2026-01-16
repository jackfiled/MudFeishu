// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Mud.Feishu.Webhook.Tests.Services;

/// <summary>
/// FeishuEventDecryptor 单元测试
/// </summary>
public class FeishuEventDecryptorTests
{
    private readonly Mock<ILogger<FeishuEventDecryptor>> _loggerMock;
    private readonly FeishuEventDecryptor _decryptor;

    public FeishuEventDecryptorTests()
    {
        _loggerMock = new Mock<ILogger<FeishuEventDecryptor>>();
        _decryptor = new FeishuEventDecryptor(_loggerMock.Object);
    }

    [Fact]
    public async Task DecryptAsync_WithValidV1Data_ShouldReturnEventData()
    {
        // Arrange
        var encryptKey = "test_encrypt_key_123456";
        var originalJson = "{\"event_type\":\"test_event\",\"event_id\":\"test_123\",\"create_time\":1234567890}";
        var encryptedData = EncryptData(originalJson, encryptKey);

        // Act
        var result = await _decryptor.DecryptAsync(encryptedData, encryptKey);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test_event", result.EventType);
        Assert.Equal("test_123", result.EventId);
    }

    [Fact]
    public async Task DecryptAsync_WithInvalidKey_ShouldReturnNull()
    {
        // Arrange
        var correctKey = "correct_key_123456";
        var wrongKey = "wrong_key_123456";
        var originalJson = "{\"event_type\":\"test_event\"}";
        var encryptedData = EncryptData(originalJson, correctKey);

        // Act
        var result = await _decryptor.DecryptAsync(encryptedData, wrongKey);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DecryptAsync_WithInvalidBase64_ShouldReturnNull()
    {
        // Arrange
        var encryptKey = "test_key";
        var invalidBase64 = "not_valid_base64!!!";

        // Act
        var result = await _decryptor.DecryptAsync(invalidBase64, encryptKey);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DecryptAsync_WithEmptyData_ShouldReturnNull()
    {
        // Arrange
        var encryptKey = "test_key";

        // Act
        var result = await _decryptor.DecryptAsync("", encryptKey);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// 辅助方法：加密数据（模拟飞书加密）
    /// </summary>
    private string EncryptData(string plainText, string encryptKey)
    {
        using var aes = Aes.Create();
        
        // 使用 SHA256 哈希密钥
        using var sha256 = SHA256.Create();
        var keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptKey));
        aes.Key = keyBytes;
        aes.Mode = CipherMode.CBC;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // 将 IV 和加密数据组合
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }
}
