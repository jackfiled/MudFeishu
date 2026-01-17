namespace FeishuOAuthDemo.Services;

/// <summary>
/// 用户服务实现（内存存储示例，生产环境应使用数据库）
/// </summary>
public class UserService : IUserService
{
    private readonly Dictionary<string, UserInfo> _users = new();
    private readonly object _lock = new();

    public async Task<string> GetOrCreateUserAsync(string openId, string unionId, string name, string? avatar, string? email)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            // 尝试通过openId查找现有用户
            var existingUser = _users.Values.FirstOrDefault(u => u.OpenId == openId);
            if (existingUser != null)
            {
                existingUser.LastLoginAt = DateTime.UtcNow;
                existingUser.Name = name;
                existingUser.Avatar = avatar;
                existingUser.Email = email;
                return existingUser.UserId;
            }

            // 创建新用户
            var userId = $"user_{Guid.NewGuid():N}";
            var newUser = new UserInfo
            {
                UserId = userId,
                OpenId = openId,
                UnionId = unionId,
                Name = name,
                Avatar = avatar,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _users[userId] = newUser;
            return userId;
        }
    }

    public async Task<UserInfo?> GetUserByIdAsync(string userId)
    {
        await Task.CompletedTask;
        lock (_lock)
        {
            return _users.TryGetValue(userId, out var user) ? user : null;
        }
    }
}
