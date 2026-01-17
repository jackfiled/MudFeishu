namespace FeishuOAuthDemo.Services;

/// <summary>
/// JWT令牌服务接口
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// 生成JWT令牌
    /// </summary>
    string GenerateToken(string openId, string unionId, string name);

    /// <summary>
    /// 验证JWT令牌
    /// </summary>
    bool ValidateToken(string token, out System.Security.Claims.ClaimsPrincipal? principal);

    /// <summary>
    /// 从令牌中提取用户信息
    /// </summary>
    (string openId, string unionId, string name)? GetUserFromToken(string token);
}
