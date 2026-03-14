namespace FeishuFileServer.Configuration;

/// <summary>
/// JWT认证配置
/// 用于配置JWT令牌的生成和验证参数
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// JWT密钥
    /// 用于签名和验证令牌
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// 令牌签发者
    /// </summary>
    public string Issuer { get; set; } = "FeishuFileServer";

    /// <summary>
    /// 令牌受众
    /// </summary>
    public string Audience { get; set; } = "FeishuFileClient";

    /// <summary>
    /// 令牌过期时间（小时）
    /// </summary>
    public int ExpirationHours { get; set; } = 24;

    /// <summary>
    /// 刷新令牌过期时间（天）
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
