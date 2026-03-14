using System.ComponentModel.DataAnnotations;

namespace FeishuFileServer.Models.DTOs;

/// <summary>
/// 登录请求
/// 用于用户登录认证
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 登录响应
/// 登录成功后返回的令牌和用户信息
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT访问令牌
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// 令牌类型（默认为 Bearer）
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// 令牌过期时间（秒）
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// 用户信息
    /// </summary>
    public UserInfo User { get; set; } = new();
}

/// <summary>
/// 刷新令牌请求
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// 刷新令牌
    /// </summary>
    [Required(ErrorMessage = "刷新令牌不能为空")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// 用户信息
/// 包含用户的基本信息
/// </summary>
public class UserInfo
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱地址
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 用户角色
    /// </summary>
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// 注册请求
/// 用于创建新用户账户
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码（至少6个字符）
    /// </summary>
    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱地址（可选）
    /// </summary>
    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// 显示名称（可选）
    /// </summary>
    [MaxLength(50)]
    public string? DisplayName { get; set; }
}

/// <summary>
/// 修改密码请求
/// 用于修改用户密码
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// 当前密码
    /// </summary>
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码（至少6个字符）
    /// </summary>
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
