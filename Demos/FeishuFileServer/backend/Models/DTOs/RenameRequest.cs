using System.ComponentModel.DataAnnotations;

namespace FeishuFileServer.Models.DTOs;

/// <summary>
/// 重命名文件请求
/// 用于更新文件的显示名称
/// </summary>
public class RenameFileRequest
{
    /// <summary>
    /// 新文件名
    /// </summary>
    [Required(ErrorMessage = "文件名不能为空")]
    [MaxLength(500, ErrorMessage = "文件名长度不能超过500个字符")]
    public string NewName { get; set; } = string.Empty;
}

/// <summary>
/// 重命名文件夹请求
/// 用于更新文件夹的显示名称
/// </summary>
public class RenameFolderRequest
{
    /// <summary>
    /// 新文件夹名
    /// </summary>
    [Required(ErrorMessage = "文件夹名不能为空")]
    [MaxLength(500, ErrorMessage = "文件夹名长度不能超过500个字符")]
    public string NewName { get; set; } = string.Empty;
}
