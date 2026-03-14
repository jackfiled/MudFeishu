using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeishuFileServer.Models;

/// <summary>
/// 文件权限实体
/// 定义文件和文件夹的访问权限
/// </summary>
[Table("FilePermissions")]
public class FilePermission
{
    /// <summary>
    /// 权限ID（主键）
    /// </summary>
    [Key]
    public long Id { get; set; }

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
    /// 用户ID
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 用户导航属性
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// 权限类型
    /// </summary>
    [Required]
    public PermissionType PermissionType { get; set; } = PermissionType.Read;

    /// <summary>
    /// 是否允许读取
    /// </summary>
    public bool CanRead { get; set; } = true;

    /// <summary>
    /// 是否允许写入
    /// </summary>
    public bool CanWrite { get; set; } = false;

    /// <summary>
    /// 是否允许删除
    /// </summary>
    public bool CanDelete { get; set; } = false;

    /// <summary>
    /// 是否允许分享
    /// </summary>
    public bool CanShare { get; set; } = false;

    /// <summary>
    /// 是否允许管理权限
    /// </summary>
    public bool CanManage { get; set; } = false;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 权限类型枚举
/// </summary>
public enum PermissionType
{
    /// <summary>
    /// 无权限
    /// </summary>
    None = 0,

    /// <summary>
    /// 只读权限
    /// </summary>
    Read = 1,

    /// <summary>
    /// 读写权限
    /// </summary>
    Write = 2,

    /// <summary>
    /// 完全控制权限
    /// </summary>
    FullControl = 3
}
