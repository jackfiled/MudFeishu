namespace FeishuFileServer.Configuration;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "FeishuFileServer";
    public string Audience { get; set; } = "FeishuFileClient";
    public int ExpirationHours { get; set; } = 24;
}
