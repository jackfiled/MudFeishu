using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeishuFileServer.Models;

/// <summary>
/// 文件夹记录实体
/// 表示飞书云盘中的文件夹信息
/// </summary>
[Table("FolderRecords")]
public class FolderRecord
{
    /// <summary>
    /// 文件夹记录ID（主键）
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 文件夹令牌（飞书云盘唯一标识）
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FolderToken { get; set; } = string.Empty;

    /// <summary>
    /// 文件夹名称
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string FolderName { get; set; } = string.Empty;

    /// <summary>
    /// 父文件夹令牌
    /// </summary>
    [MaxLength(100)]
    public string? ParentFolderToken { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否已删除（软删除标记）
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 所属用户ID
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 所属用户导航属性
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
