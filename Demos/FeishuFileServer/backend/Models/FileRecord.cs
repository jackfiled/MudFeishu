using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeishuFileServer.Models;

[Table("FileRecords")]
public class FileRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FileToken { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FolderToken { get; set; }

    [MaxLength(100)]
    public string? VersionToken { get; set; }

    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [MaxLength(100)]
    public string? MimeType { get; set; }

    [MaxLength(50)]
    public string? FileMD5 { get; set; }

    public DateTime UploadTime { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
