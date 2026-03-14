using System.ComponentModel.DataAnnotations;

namespace FeishuFileServer.Models.DTOs;

/// <summary>
/// 创建分享请求
/// </summary>
public class CreateShareRequest
{
    /// <summary>
    /// 资源类型（File/Folder）
    /// </summary>
    [Required(ErrorMessage = "资源类型不能为空")]
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// 资源令牌
    /// </summary>
    [Required(ErrorMessage = "资源令牌不能为空")]
    public string ResourceToken { get; set; } = string.Empty;

    /// <summary>
    /// 访问密码（可选）
    /// </summary>
    [MaxLength(50, ErrorMessage = "密码长度不能超过50个字符")]
    public string? Password { get; set; }

    /// <summary>
    /// 过期时间（可选）
    /// </summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 最大访问次数（可选）
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "最大访问次数必须大于0")]
    public int? MaxAccessCount { get; set; }

    /// <summary>
    /// 是否允许下载
    /// </summary>
    public bool AllowDownload { get; set; } = true;
}

/// <summary>
/// 更新分享请求
/// </summary>
public class UpdateShareRequest
{
    /// <summary>
    /// 访问密码（可选）
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 过期时间（可选）
    /// </summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 最大访问次数（可选）
    /// </summary>
    public int? MaxAccessCount { get; set; }

    /// <summary>
    /// 是否允许下载
    /// </summary>
    public bool AllowDownload { get; set; } = true;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 分享响应
/// </summary>
public class ShareResponse
{
    /// <summary>
    /// 分享ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 分享码
    /// </summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>
    /// 分享链接
    /// </summary>
    public string ShareLink { get; set; } = string.Empty;

    /// <summary>
    /// 资源类型
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称
    /// </summary>
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// 是否需要密码
    /// </summary>
    public bool RequirePassword { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime? ExpireTime { get; set; }

    /// <summary>
    /// 是否允许下载
    /// </summary>
    public bool AllowDownload { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }
}

/// <summary>
/// 分享内容响应
/// </summary>
public class ShareContentResponse
{
    /// <summary>
    /// 资源类型
    /// </summary>
    public string ResourceType { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称
    /// </summary>
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// 文件信息（如果是文件）
    /// </summary>
    public FileInfoResponse? File { get; set; }

    /// <summary>
    /// 文件夹内容（如果是文件夹）
    /// </summary>
    public FolderContentsResponse? FolderContents { get; set; }

    /// <summary>
    /// 是否允许下载
    /// </summary>
    public bool AllowDownload { get; set; }
}

/// <summary>
/// 分享列表响应
/// </summary>
public class ShareListResponse
{
    /// <summary>
    /// 分享列表
    /// </summary>
    public List<ShareResponse> Shares { get; set; } = new();

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }
}
