namespace FeishuOAuthDemo.Models;

/// <summary>
/// OAuth回调请求
/// </summary>
public class AuthCallbackRequest
{
    /// <summary>
    /// 授权码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 状态参数（防CSRF）
    /// </summary>
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// 登录响应
/// </summary>
public class LoginResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public UserInfoResponse? User { get; set; }
}

/// <summary>
/// 用户信息响应
/// </summary>
public class UserInfoResponse
{
    public string OpenId { get; set; } = string.Empty;
    public string UnionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
}

/// <summary>
/// 授权URL响应
/// </summary>
public class AuthUrlResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Url { get; set; }
    public string? State { get; set; }
}

/// <summary>
/// 刷新Token请求
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// 刷新Token响应
/// </summary>
public class RefreshTokenResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}

/// <summary>
/// 令牌状态响应
/// </summary>
public class TokenStatusResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public bool HasValidToken { get; set; }
    public bool CanRefresh { get; set; }
    public TokenExpirationInfo? TokenInfo { get; set; }
}

/// <summary>
/// 令牌过期信息
/// </summary>
public class TokenExpirationInfo
{
    public DateTime? AccessTokenExpiresAt { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public bool AccessTokenExpired { get; set; }
    public bool RefreshTokenExpired { get; set; }
}

/// <summary>
/// 登出响应
/// </summary>
public class LogoutResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
