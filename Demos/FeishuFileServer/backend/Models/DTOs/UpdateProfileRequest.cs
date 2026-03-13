using System.ComponentModel.DataAnnotations;

namespace FeishuFileServer.Models.DTOs;

/// <summary>
/// 更新用户资料请求
/// 用于更新用户的邮箱和显示名称
/// </summary>
public class UpdateProfileRequest
{
    /// <br>
    /// 邮箱地址
    /// </summary>
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    [StringLength(50, MinimumLength = 1)]
    public string? DisplayName { get; set; }
}
