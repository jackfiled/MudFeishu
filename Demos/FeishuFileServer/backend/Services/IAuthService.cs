using FeishuFileServer.Models.DTOs;

namespace FeishuFileServer.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> RegisterAsync(RegisterRequest request);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<UserInfo?> GetUserInfoAsync(int userId);
    Task<UserInfo?> UpdateProfileAsync(int userId, UpdateProfileRequest request);
}
