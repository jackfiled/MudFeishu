using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeishuFileServer.Models;

[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? DisplayName { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }
}

public enum UserRole
{
    User = 0,
    Admin = 1
}
