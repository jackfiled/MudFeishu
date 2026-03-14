using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeishuFileServer.Models;

/// <summary>
/// 刷新令牌实体
/// 用于JWT令牌刷新机制
/// </summary>
[Table("RefreshTokens")]
public class RefreshToken
{
    /// <summary>
    /// 令牌ID（主键）
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 令牌值
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 关联的用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 用户导航属性
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpireTime { get; set; }

    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// 撤销时间
    /// </summary>
    public DateTime? RevokedTime { get; set; }

    /// <summary>
    /// 是否已使用
    /// </summary>
    public bool IsUsed { get; set; } = false;

    /// <summary>
    /// 使用时间
    /// </summary>
    public DateTime? UsedTime { get; set; }

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid => !IsRevoked && !IsUsed && ExpireTime > DateTime.UtcNow;
}
