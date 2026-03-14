using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeishuFileServer.Models;

/// <summary>
/// 操作日志实体
/// 记录用户对文件和文件夹的操作历史
/// </summary>
[Table("OperationLogs")]
public class OperationLog
{
    /// <summary>
    /// 日志ID（主键）
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [MaxLength(50)]
    public string? Username { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 资源类型（File/Folder）
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// 资源令牌
    /// </summary>
    [MaxLength(100)]
    public string? ResourceToken { get; set; }

    /// <summary>
    /// 资源名称
    /// </summary>
    [MaxLength(500)]
    public string? ResourceName { get; set; }

    /// <summary>
    /// 操作详情（JSON格式）
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperationTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// 错误信息
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 操作类型枚举
/// </summary>
public static class OperationTypes
{
    public const string Upload = "Upload";
    public const string Download = "Download";
    public const string Delete = "Delete";
    public const string Restore = "Restore";
    public const string Rename = "Rename";
    public const string Move = "Move";
    public const string Copy = "Copy";
    public const string CreateFolder = "CreateFolder";
    public const string DeleteFolder = "DeleteFolder";
    public const string RestoreFolder = "RestoreFolder";
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string Register = "Register";
}

/// <summary>
/// 资源类型枚举
/// </summary>
public static class ResourceTypes
{
    public const string File = "File";
    public const string Folder = "Folder";
    public const string User = "User";
}
