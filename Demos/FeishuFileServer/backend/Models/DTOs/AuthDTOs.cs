using System.ComponentModel.DataAnnotations;

namespace FeishuFileServer.Models.DTOs;

public class LoginRequest
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public UserInfo User { get; set; } = new();
}

public class UserInfo
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? DisplayName { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? DisplayName { get; set; }
}
