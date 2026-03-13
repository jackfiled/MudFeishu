namespace FeishuFileServer.Models.DTOs;

/// <summary>
/// 文件上传请求
/// </summary>
public class FileUploadRequest
{
    /// <summary>
    /// 上传的文件
    /// </summary>
    public IFormFile? File { get; set; }

    /// <summary>
    /// 目标文件夹令牌
    /// </summary>
    public string? FolderToken { get; set; }
}

/// <summary>
/// 文件上传响应
/// </summary>
public class FileUploadResponse
{
    /// <summary>
    /// 文件令牌
    /// </summary>
    public string FileToken { get; set; } = string.Empty;

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME类型
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// 文件MD5哈希值
    /// </summary>
    public string? FileMD5 { get; set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTime UploadTime { get; set; }
}

/// <summary>
/// 文件信息响应
/// </summary>
public class FileInfoResponse
{
    /// <summary>
    /// 文件记录ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 文件令牌
    /// </summary>
    public string FileToken { get; set; } = string.Empty;

    /// <summary>
    /// 所属文件夹令牌
    /// </summary>
    public string? FolderToken { get; set; }

    /// <summary>
    /// 当前版本令牌
    /// </summary>
    public string? VersionToken { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME类型
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// 文件MD5哈希值
    /// </summary>
    public string? FileMD5 { get; set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTime UploadTime { get; set; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted { get; set; }
}

/// <summary>
/// 文件列表响应
/// </summary>
public class FileListResponse
{
    /// <summary>
    /// 文件列表
    /// </summary>
    public List<FileInfoResponse> Files { get; set; } = new();

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; }
}
