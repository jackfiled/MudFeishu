using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeishuFileServer.Models;

[Table("VersionRecords")]
public class VersionRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FileToken { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string VersionToken { get; set; } = string.Empty;

    public int VersionNumber { get; set; }

    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [MaxLength(50)]
    public string? FileMD5 { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public bool IsCurrentVersion { get; set; } = false;
}
