using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FeishuFileServer.Configuration;
using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FeishuFileServer.Services;

public class AuthService : IAuthService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;

    public AuthService(FeishuFileDbContext dbContext, IOptions<JwtSettings> jwtSettings)
    {
        _dbContext = dbContext;
        _jwtSettings = jwtSettings.Value;
    }

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

    public async Task<UserInfo?> GetUserInfoAsync(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        return user == null ? null : MapToUserInfo(user);
    }

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
