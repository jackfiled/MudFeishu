using System.ComponentModel.DataAnnotations;

namespace FeishuFileServer.Models.DTOs;

/// <summary>
/// 批量删除请求
/// </summary>
public class BatchDeleteRequest
{
    /// <summary>
    /// 要删除的文件令牌列表
    /// </summary>
    public List<string> FileTokens { get; set; } = new();

    /// <summary>
    /// 要删除的文件夹令牌列表
    /// </summary>
    public List<string> FolderTokens { get; set; } = new();
}

/// <summary>
/// 批量移动请求
/// </summary>
public class BatchMoveRequest
{
    /// <summary>
    /// 要移动的文件令牌列表
    /// </summary>
    public List<string> FileTokens { get; set; } = new();

    /// <summary>
    /// 要移动的文件夹令牌列表
    /// </summary>
    public List<string> FolderTokens { get; set; } = new();

    /// <summary>
    /// 目标文件夹令牌
    /// </summary>
    public string? TargetFolderToken { get; set; }
}

/// <summary>
/// 批量复制请求
/// </summary>
public class BatchCopyRequest
{
    /// <summary>
    /// 要复制的文件令牌列表
    /// </summary>
    public List<string> FileTokens { get; set; } = new();

    /// <summary>
    /// 要复制的文件夹令牌列表
    /// </summary>
    public List<string> FolderTokens { get; set; } = new();

    /// <summary>
    /// 目标文件夹令牌
    /// </summary>
    public string? TargetFolderToken { get; set; }
}

/// <summary>
/// 批量下载请求
/// </summary>
public class BatchDownloadRequest
{
    /// <summary>
    /// 要下载的文件令牌列表
    /// </summary>
    public List<string> FileTokens { get; set; } = new();

    /// <summary>
    /// 要下载的文件夹令牌列表
    /// </summary>
    public List<string> FolderTokens { get; set; } = new();
}

/// <summary>
/// 批量恢复请求
/// </summary>
public class BatchRestoreRequest
{
    /// <summary>
    /// 要恢复的文件令牌列表
    /// </summary>
    public List<string> FileTokens { get; set; } = new();

    /// <summary>
    /// 要恢复的文件夹令牌列表
    /// </summary>
    public List<string> FolderTokens { get; set; } = new();
}

/// <summary>
/// 批量分享请求
/// </summary>
public class BatchShareRequest
{
    /// <summary>
    /// 要分享的文件令牌列表
    /// </summary>
    public List<string> FileTokens { get; set; } = new();

    /// <summary>
    /// 要分享的文件夹令牌列表
    /// </summary>
    public List<string> FolderTokens { get; set; } = new();

    /// <summary>
    /// 分享链接有效期（天）
    /// </summary>
    public int? ExpireDays { get; set; }
}

/// <summary>
/// 批量操作响应
/// </summary>
public class BatchOperationResponse
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 成功数量
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败数量
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 失败的项目详情
    /// </summary>
    public List<BatchOperationError> Errors { get; set; } = new();
}

/// <summary>
/// 批量操作错误
/// </summary>
public class BatchOperationError
{
    /// <summary>
    /// 资源令牌
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 分片上传初始化请求
/// </summary>
public class InitChunkUploadRequest
{
    /// <summary>
    /// 文件名
    /// </summary>
    [Required(ErrorMessage = "文件名不能为空")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件总大小（字节）
    /// </summary>
    [Required(ErrorMessage = "文件大小不能为空")]
    [Range(1, long.MaxValue, ErrorMessage = "文件大小必须大于0")]
    public long FileSize { get; set; }

    /// <summary>
    /// 文件MD5值
    /// </summary>
    public string? FileMD5 { get; set; }

    /// <summary>
    /// 分片大小（字节），默认5MB
    /// </summary>
    public int ChunkSize { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// 目标文件夹令牌
    /// </summary>
    public string? FolderToken { get; set; }
}

/// <summary>
/// 分片上传初始化响应
/// </summary>
public class InitChunkUploadResponse
{
    /// <summary>
    /// 上传ID
    /// </summary>
    public string UploadId { get; set; } = string.Empty;

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件总大小
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 分片大小
    /// </summary>
    public int ChunkSize { get; set; }

    /// <summary>
    /// 总分片数
    /// </summary>
    public int TotalChunks { get; set; }
}

/// <summary>
/// 分片上传请求
/// </summary>
public class ChunkUploadRequest
{
    /// <summary>
    /// 上传ID
    /// </summary>
    [Required(ErrorMessage = "上传ID不能为空")]
    public string UploadId { get; set; } = string.Empty;

    /// <summary>
    /// 分片序号（从1开始）
    /// </summary>
    [Required(ErrorMessage = "分片序号不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "分片序号必须大于0")]
    public int ChunkNumber { get; set; }

    /// <summary>
    /// 分片MD5值
    /// </summary>
    public string? ChunkMD5 { get; set; }
}

/// <summary>
/// 分片上传响应
/// </summary>
public class ChunkUploadResponse
{
    /// <summary>
    /// 上传ID
    /// </summary>
    public string UploadId { get; set; } = string.Empty;

    /// <summary>
    /// 分片序号
    /// </summary>
    public int ChunkNumber { get; set; }

    /// <summary>
    /// 是否上传完成
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// 已上传分片数
    /// </summary>
    public int UploadedChunks { get; set; }

    /// <summary>
    /// 总分片数
    /// </summary>
    public int TotalChunks { get; set; }

    /// <summary>
    /// 上传进度（百分比）
    /// </summary>
    public double Progress { get; set; }

    /// <summary>
    /// 文件令牌（上传完成后返回）
    /// </summary>
    public string? FileToken { get; set; }
}

/// <summary>
/// 完成分片上传请求
/// </summary>
public class CompleteChunkUploadRequest
{
    /// <summary>
    /// 上传ID
    /// </summary>
    [Required(ErrorMessage = "上传ID不能为空")]
    public string UploadId { get; set; } = string.Empty;
}
