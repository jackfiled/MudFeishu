namespace FeishuOAuthDemo.Models;

/// <summary>
/// OAuth配置选项
/// </summary>
public class OAuthOptions
{
    /// <summary>
    /// 重定向URI（必须与飞书开放平台配置一致）
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// JWT配置
    /// </summary>
    public JwtOptions Jwt { get; set; } = new();

    /// <summary>
    /// State参数过期时间（分钟）
    /// </summary>
    public int StateExpirationMinutes { get; set; } = 5;
}

/// <summary>
/// JWT配置选项
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// JWT密钥（至少32字符）
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// 签发者
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 受众
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间（分钟）
    /// </summary>
    public int ExpirationMinutes { get; set; } = 1440;
}
