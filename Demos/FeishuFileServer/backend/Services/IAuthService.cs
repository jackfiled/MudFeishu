using FeishuFileServer.Models.DTOs;

namespace FeishuFileServer.Services;

/// <summary>
/// 认证服务接口
/// 提供用户登录、注册、密码修改和个人信息管理功能
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 用户登录
    /// <para>验证用户名和密码，成功后生成JWT令牌</para>
    /// </summary>
    /// <param name="request">登录请求，包含用户名和密码</param>
    /// <returns>登录成功返回包含令牌和用户信息的响应，失败返回 null</returns>
    Task<LoginResponse?> LoginAsync(LoginRequest request);

    /// <summary>
    /// 用户注册
    /// <para>创建新用户账户并生成JWT令牌</para>
    /// </summary>
    /// <param name="request">注册请求，包含用户名、密码等信息</param>
    /// <returns>注册成功返回包含令牌和用户信息的响应</returns>
    /// <exception cref="InvalidOperationException">当用户名或邮箱已存在时抛出</exception>
    Task<LoginResponse?> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// 修改用户密码
    /// <para>验证当前密码后更新为新密码</para>
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">修改密码请求，包含当前密码和新密码</param>
    /// <returns>修改成功返回 true，失败返回 false</returns>
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户信息，用户不存在时返回 null</returns>
    Task<UserInfo?> GetUserInfoAsync(int userId);

    /// <summary>
    /// 更新用户资料
    /// <para>更新用户的邮箱和显示名称</para>
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">更新资料请求</param>
    /// <returns>更新后的用户信息，用户不存在时返回 null</returns>
    /// <exception cref="InvalidOperationException">当邮箱已被其他用户使用时抛出</exception>
    Task<UserInfo?> UpdateProfileAsync(int userId, UpdateProfileRequest request);
}
