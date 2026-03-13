namespace FeishuFileServer.Models.DTOs;

/// <summary>
/// 版本响应
/// </summary>
public class VersionResponse
{
    /// <summary>
    /// 版本记录ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的文件令牌
    /// </summary>
    public string FileToken { get; set; } = string.Empty;

    /// <summary>
    /// 版本令牌
    /// </summary>
    public string VersionToken { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件MD5哈希值
    /// </summary>
    public string? FileMD5 { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 是否为当前版本
    /// </summary>
    public bool IsCurrentVersion { get; set; }
}

/// <summary>
/// 版本列表响应
/// </summary>
public class VersionListResponse
{
    /// <summary>
    /// 版本列表
    /// </summary>
    public List<VersionResponse> Versions { get; set; } = new();

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// 版本创建响应
/// </summary>
public class VersionCreateResponse
{
    /// <summary>
    /// 版本令牌
    /// </summary>
    public string VersionToken { get; set; } = string.Empty;

    /// <summary>
    /// 版本号
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }
}
