namespace FeishuOAuthDemo.Services;

/// <summary>
/// State存储服务接口（用于防CSRF攻击）
/// </summary>
public interface IStateStorageService
{
    /// <summary>
    /// 生成并存储state
    /// </summary>
    string GenerateState();

    /// <summary>
    /// 验证state
    /// </summary>
    bool ValidateState(string state);

    /// <summary>
    /// 移除已使用的state
    /// </summary>
    void RemoveState(string state);

    /// <summary>
    /// 清理过期的state
    /// </summary>
    void CleanExpiredStates();
}
