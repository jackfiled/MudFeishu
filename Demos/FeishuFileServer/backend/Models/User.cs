using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeishuFileServer.Models;

/// <summary>
/// 用户实体
/// 表示系统中的用户账户
/// </summary>
[Table("Users")]
public class User
{
    /// <summary>
    /// 用户ID（主键）
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 用户名（唯一）
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码哈希值
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱地址（可选，唯一）
    /// </summary>
    [MaxLength(100)]
    public string? Email { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    [MaxLength(50)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// 用户角色
    /// </summary>
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// 账户是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 账户创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 用户拥有的文件集合
    /// </summary>
    public ICollection<FileRecord> Files { get; set; } = new List<FileRecord>();

    /// <summary>
    /// 用户拥有的文件夹集合
    /// </summary>
    public ICollection<FolderRecord> Folders { get; set; } = new List<FolderRecord>();
}

/// <summary>
/// 用户角色枚举
/// </summary>
public enum UserRole
{
    /// <summary>
    /// 普通用户
    /// </summary>
    User = 0,

    /// <summary>
    /// 管理员
    /// </summary>
    Admin = 1
}
