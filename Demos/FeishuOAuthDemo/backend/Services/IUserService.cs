namespace FeishuOAuthDemo.Services;

/// <summary>
/// 用户服务接口（示例：实际项目中应该连接数据库）
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 根据飞书用户信息获取或创建本地用户
    /// </summary>
    Task<string> GetOrCreateUserAsync(string openId, string unionId, string name, string? avatar, string? email);

    /// <summary>
    /// 根据用户ID获取用户信息
    /// </summary>
    Task<UserInfo?> GetUserByIdAsync(string userId);
}

/// <summary>
/// 用户信息（示例模型）
/// </summary>
public class UserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string OpenId { get; set; } = string.Empty;
    public string UnionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
