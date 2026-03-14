using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeishuFileServer.Models;

/// <summary>
/// 文件分享记录实体
/// 记录文件和文件夹的分享信息
/// </summary>
[Table("ShareRecords")]
public class ShareRecord
{
    /// <summary>
    /// 分享ID（主键）
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 分享码（用于访问分享链接）
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源类型（File/Folder）
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// 资源令牌
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ResourceToken { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// 创建者用户ID
    /// </summary>
    public int CreatorId { get; set; }

    /// <summary>
    /// 创建者导航属性
    /// </summary>
    [ForeignKey(nameof(CreatorId))]
    public User? Creator { get; set; }

    /// <summary>
    /// 访问密码（可选）
    /// </summary>
    [MaxLength(100)]
    public string? Password { get; set; }

    /// <summary>
    /// 过期时间（null表示永不过期）
    /// </summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 最大访问次数（null表示无限制）
    /// </summary>
    public int? MaxAccessCount { get; set; }

    /// <summary>
    /// 当前访问次数
    /// </summary>
    public int AccessCount { get; set; } = 0;

    /// <summary>
    /// 是否允许下载
    /// </summary>
    public bool AllowDownload { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;
}
