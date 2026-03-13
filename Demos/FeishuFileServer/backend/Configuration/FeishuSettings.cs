namespace FeishuFileServer.Configuration;

public class FeishuSettings
{
    public string AppId { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string TenantKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://open.feishu.cn";
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
}
