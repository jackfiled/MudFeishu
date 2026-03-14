using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeishuFileServer.Models;

/// <summary>
/// 文件记录实体
/// 表示存储在飞书云盘中的文件信息
/// </summary>
[Table("FileRecords")]
public class FileRecord
{
    /// <summary>
    /// 文件记录ID（主键）
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 文件令牌（飞书云盘唯一标识）
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FileToken { get; set; } = string.Empty;

    /// <summary>
    /// 所属文件夹令牌
    /// </summary>
    [MaxLength(100)]
    public string? FolderToken { get; set; }

    /// <summary>
    /// 当前版本令牌
    /// </summary>
    [MaxLength(100)]
    public string? VersionToken { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME类型
    /// </summary>
    [MaxLength(100)]
    public string? MimeType { get; set; }

    /// <summary>
    /// 文件MD5哈希值（用于去重）
    /// </summary>
    [MaxLength(50)]
    public string? FileMD5 { get; set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTime UploadTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否已删除（软删除标记）
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 删除时间（软删除时记录）
    /// </summary>
    public DateTime? DeletedTime { get; set; }

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
