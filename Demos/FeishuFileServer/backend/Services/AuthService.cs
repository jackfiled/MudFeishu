using FeishuFileServer.Configuration;
using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// 初始化认证服务实例
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="jwtSettings">JWT配置选项</param>
    /// <param name="logger">日志记录器</param>
    public AuthService(FeishuFileDbContext dbContext, IOptions<JwtSettings> jwtSettings, ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// 用户登录
    /// 验证用户名和密码，成功后生成JWT令牌和刷新令牌
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
        var refreshToken = await GenerateRefreshTokenAsync(user.Id);

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            TokenType = "Bearer",
            ExpiresIn = _jwtSettings.ExpirationHours * 3600,
            User = MapToUserInfo(user)
        };
    }

    /// <summary>
    /// 用户注册
    /// 创建新用户账户并生成JWT令牌和刷新令牌
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
        var refreshToken = await GenerateRefreshTokenAsync(user.Id);

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            TokenType = "Bearer",
            ExpiresIn = _jwtSettings.ExpirationHours * 3600,
            User = MapToUserInfo(user)
        };
    }

    /// <summary>
    /// 刷新访问令牌
    /// 使用刷新令牌获取新的访问令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns>新的登录响应</returns>
    public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken == null || !storedToken.IsValid)
        {
            _logger.LogWarning("Invalid refresh token attempt: {Token}", refreshToken);
            throw new UnauthorizedAccessException("无效的刷新令牌");
        }

        storedToken.IsUsed = true;
        storedToken.UsedTime = DateTime.UtcNow;

        var user = storedToken.User;
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("用户不存在或已禁用");
        }

        var newToken = GenerateJwtToken(user);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.Id);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Token refreshed for user {UserId}", user.Id);

        return new LoginResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken.Token,
            TokenType = "Bearer",
            ExpiresIn = _jwtSettings.ExpirationHours * 3600,
            User = MapToUserInfo(user)
        };
    }

    /// <summary>
    /// 撤销刷新令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="userId">用户ID</param>
    public async Task RevokeRefreshTokenAsync(string refreshToken, int userId)
    {
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

        if (storedToken != null)
        {
            storedToken.IsRevoked = true;
            storedToken.RevokedTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Refresh token revoked for user {UserId}", userId);
        }
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
    /// 生成刷新令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>刷新令牌实体</returns>
    private async Task<RefreshToken> GenerateRefreshTokenAsync(int userId)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = userId,
            CreatedTime = DateTime.UtcNow,
            ExpireTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IsRevoked = false,
            IsUsed = false
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        return refreshToken;
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
