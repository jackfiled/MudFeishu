using FeishuFileServer.Models.DTOs;

namespace FeishuFileServer.Services;

/// <summary>
/// 文件服务接口
/// 提供文件的上传、下载、删除、查询、移动和复制功能
/// </summary>
public interface IFileService
{
    /// <summary>
    /// 上传文件
    /// 将文件上传到飞书云存储，并在本地数据库创建记录
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <param name="folderToken">目标文件夹令牌，为空表示根目录</param>
    /// <param name="userId">用户ID，可选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上传响应，包含文件令牌和基本信息</returns>
    /// <exception cref="ArgumentException">文件为空、类型不允许或大小超限时抛出</exception>
    Task<FileUploadResponse> UploadFileAsync(IFormFile file, string? folderToken, int? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载文件
    /// 从飞书云存储下载指定文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="versionToken">版本令牌，可选，指定时下载特定版本</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容字节数组</returns>
    /// <exception cref="KeyNotFoundException">文件不存在时抛出</exception>
    Task<byte[]> DownloadFileAsync(string fileToken, string? versionToken = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除文件
    /// 从飞书云存储和本地数据库删除文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="KeyNotFoundException">文件不存在时抛出</exception>
    Task DeleteFileAsync(string fileToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件信息，文件不存在时返回 null</returns>
    Task<FileInfoResponse?> GetFileAsync(string fileToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文件列表
    /// 支持按文件夹和用户筛选，支持分页
    /// </summary>
    /// <param name="folderToken">文件夹令牌，可选</param>
    /// <param name="userId">用户ID，可选</param>
    /// <param name="page">页码，从1开始</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件列表响应</returns>
    Task<FileListResponse> GetFilesAsync(string? folderToken = null, int? userId = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移动文件
    /// 将文件移动到目标文件夹
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="destFolderToken">目标文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="KeyNotFoundException">文件不存在时抛出</exception>
    Task MoveFileAsync(string fileToken, string destFolderToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 复制文件
    /// 将文件复制到目标文件夹
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="destFolderToken">目标文件夹令牌</param>
    /// <param name="newName">新文件名，可选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <exception cref="KeyNotFoundException">文件不存在时抛出</exception>
    Task CopyFileAsync(string fileToken, string destFolderToken, string? newName = null, CancellationToken cancellationToken = default);
}
