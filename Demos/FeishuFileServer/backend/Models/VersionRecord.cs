using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeishuFileServer.Models;

/// <summary>
/// 版本记录实体
/// 表示文件的历史版本信息
/// </summary>
[Table("VersionRecords")]
public class VersionRecord
{
    /// <summary>
    /// 版本记录ID（主键）
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 关联的文件令牌
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FileToken { get; set; } = string.Empty;

    /// <summary>
    /// 版本令牌（唯一标识）
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string VersionToken { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    public int VersionNumber { get; set; }

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
    /// 文件MD5哈希值
    /// </summary>
    [MaxLength(50)]
    public string? FileMD5 { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否为当前版本
    /// </summary>
    public bool IsCurrentVersion { get; set; } = false;

    /// <summary>
    /// 飞书版本令牌
    /// 用于关联飞书云盘中的版本记录
    /// </summary>
    [MaxLength(100)]
    public string? FeishuVersionToken { get; set; }
}
