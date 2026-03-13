using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeishuFileServer.Models;

[Table("FolderRecords")]
public class FolderRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FolderToken { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string FolderName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ParentFolderToken { get; set; }

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
