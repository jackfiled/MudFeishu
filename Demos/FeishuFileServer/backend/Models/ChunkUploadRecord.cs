using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeishuFileServer.Models;

/// <summary>
/// 分片上传记录实体
/// 用于跟踪大文件分片上传进度
/// </summary>
[Table("ChunkUploadRecords")]
public class ChunkUploadRecord
{
    /// <summary>
    /// 记录ID（主键）
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 上传ID（唯一标识一次上传任务）
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string UploadId { get; set; } = string.Empty;

    /// <summary>
    /// 文件名
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件总大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件MD5值
    /// </summary>
    [MaxLength(64)]
    public string? FileMD5 { get; set; }

    /// <summary>
    /// 分片大小（字节）
    /// </summary>
    public int ChunkSize { get; set; }

    /// <summary>
    /// 总分片数
    /// </summary>
    public int TotalChunks { get; set; }

    /// <summary>
    /// 已上传分片数
    /// </summary>
    public int UploadedChunks { get; set; } = 0;

    /// <summary>
    /// 已上传的分片序号列表（JSON格式）
    /// </summary>
    public string? UploadedChunkNumbers { get; set; }

    /// <summary>
    /// 目标文件夹令牌
    /// </summary>
    [MaxLength(100)]
    public string? FolderToken { get; set; }

    /// <summary>
    /// 上传者用户ID
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
    /// 最后更新时间
    /// </summary>
    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// 是否已取消
    /// </summary>
    public bool IsCancelled { get; set; } = false;

    /// <summary>
    /// 生成的文件令牌（上传完成后）
    /// </summary>
    [MaxLength(100)]
    public string? FileToken { get; set; }
}
