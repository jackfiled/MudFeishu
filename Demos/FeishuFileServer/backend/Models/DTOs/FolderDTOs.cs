namespace FeishuFileServer.Models.DTOs;

/// <summary>
/// 文件夹创建请求
/// </summary>
public class FolderCreateRequest
{
    /// <summary>
    /// 文件夹名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 父文件夹令牌
    /// </summary>
    public string? ParentFolderToken { get; set; }
}

/// <summary>
/// 文件夹更新请求
/// </summary>
public class FolderUpdateRequest
{
    /// <summary>
    /// 文件夹名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 父文件夹令牌
    /// </summary>
    public string? ParentFolderToken { get; set; }
}

/// <summary>
/// 文件夹响应
/// </summary>
public class FolderResponse
{
    /// <summary>
    /// 文件夹记录ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 文件夹令牌
    /// </summary>
    public string FolderToken { get; set; } = string.Empty;

    /// <summary>
    /// 文件夹名称
    /// </summary>
    public string FolderName { get; set; } = string.Empty;

    /// <summary>
    /// 父文件夹令牌
    /// </summary>
    public string? ParentFolderToken { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted { get; set; }
}

/// <summary>
/// 文件夹列表响应
/// </summary>
public class FolderListResponse
{
    /// <summary>
    /// 文件夹列表
    /// </summary>
    public List<FolderResponse> Folders { get; set; } = new();

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

/// <summary>
/// 文件夹内容响应
/// </summary>
public class FolderContentsResponse
{
    /// <summary>
    /// 子文件夹列表
    /// </summary>
    public List<FolderResponse> Folders { get; set; } = new();

    /// <summary>
    /// 文件列表
    /// </summary>
    public List<FileInfoResponse> Files { get; set; } = new();
}
