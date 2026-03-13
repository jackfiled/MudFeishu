// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuFileServer.Configuration;
using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FeishuFileServer.Services;

/// <summary>
/// 认证服务实现
/// 提供用户登录、注册、密码修改和个人信息管理功能的具体实现
/// </summary>
public class AuthService : IAuthService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;

    /// <summary>
    /// 初始化认证服务实例
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="jwtSettings">JWT配置选项</param>
    public AuthService(FeishuFileDbContext dbContext, IOptions<JwtSettings> jwtSettings)
    {
        _dbContext = dbContext;
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// 用户登录
    /// 验证用户名和密码，成功后生成JWT令牌
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <returns>登录响应或null</returns>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return new LoginResponse
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresIn = _jwtSettings.ExpirationHours * 3600,
            User = MapToUserInfo(user)
        };
    }

    /// <summary>
    /// 用户注册
    /// 创建新用户账户并生成JWT令牌
    /// </summary>
    /// <param name="request">注册请求</param>
    /// <returns>登录响应</returns>
    /// <exception cref="InvalidOperationException">用户名或邮箱已存在</exception>
    public async Task<LoginResponse?> RegisterAsync(RegisterRequest request)
    {
        if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username))
        {
            throw new InvalidOperationException("用户名已存在");
        }

        if (!string.IsNullOrEmpty(request.Email) && await _dbContext.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new InvalidOperationException("邮箱已被使用");
        }

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Email = request.Email,
            DisplayName = request.DisplayName ?? request.Username,
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return new LoginResponse
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresIn = _jwtSettings.ExpirationHours * 3600,
            User = MapToUserInfo(user)
        };
    }

    /// <summary>
    /// 修改用户密码
    /// 验证当前密码后更新为新密码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">修改密码请求</param>
    /// <returns>是否修改成功</returns>
    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return false;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户信息</returns>
    public async Task<UserInfo?> GetUserInfoAsync(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        return user == null ? null : MapToUserInfo(user);
    }

    /// <summary>
    /// 更新用户资料
    /// 更新用户的邮箱和显示名称
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="request">更新资料请求</param>
    /// <returns>更新后的用户信息</returns>
    public async Task<UserInfo?> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email && u.Id != userId))
            {
                throw new InvalidOperationException("邮箱已被使用");
            }
            user.Email = request.Email;
        }

        if (!string.IsNullOrEmpty(request.DisplayName))
        {
            user.DisplayName = request.DisplayName;
        }

        await _dbContext.SaveChangesAsync();

        return MapToUserInfo(user);
    }

    /// <summary>
    /// 生成JWT令牌
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>JWT令牌字符串</returns>
    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.Now.AddHours(_jwtSettings.ExpirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 将用户实体映射为用户信息DTO
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <returns>用户信息DTO</returns>
    private static UserInfo MapToUserInfo(User user)
    {
        return new UserInfo
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString()
        };
    }
}
