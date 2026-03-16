using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 文件管理控制器
/// 提供文件的上传、下载、查询、删除等功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : FeishuControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;

    /// <summary>
    /// 初始化文件控制器
    /// </summary>
    /// <param name="fileService">文件服务</param>
    /// <param name="logger">日志记录器</param>
    public FilesController(IFileService fileService, ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="file">要上传的文件</param>
    /// <param name="folderToken">目标文件夹Token（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上传结果，包含文件Token和基本信息</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<FileUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<FileUploadResponse>>> UploadFile(
        IFormFile file,
        [FromForm] string? folderToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _fileService.UploadFileAsync(file, folderToken, userId, cancellationToken);
            return Success(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequestResult<FileUploadResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return ServerError<FileUploadResponse>("Error uploading file");
        }
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="fileToken">文件Token</param>
    /// <param name="versionToken">版本Token（可选，用于下载指定版本）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容</returns>
    [HttpGet("{fileToken}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(
        string fileToken,
        [FromQuery] string? versionToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var fileInfo = await _fileService.GetFileAsync(fileToken, cancellationToken);
            if (fileInfo == null)
            {
                return NotFoundError("File not found");
            }

            var content = await _fileService.DownloadFileAsync(fileToken, versionToken, cancellationToken);

            var mimeType = fileInfo.MimeType ?? "application/octet-stream";

            return File(content, mimeType, fileInfo.FileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundError("File not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileToken}", fileToken);
            return ServerErrorResult("Error downloading file");
        }
    }

    /// <summary>
    /// 获取文件列表
    /// </summary>
    /// <param name="folderToken">文件夹Token（可选，不传则获取根目录文件）</param>
    /// <param name="search">搜索关键词（可选）</param>
    /// <param name="page">页码，默认1</param>
    /// <param name="pageSize">每页数量，默认20</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件列表响应</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<FileListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FileListResponse>>> GetFiles(
        [FromQuery] string? folderToken,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _fileService.GetFilesAsync(folderToken, userId, search, page, pageSize, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 重命名文件
    /// </summary>
    /// <param name="fileToken">文件Token</param>
    /// <param name="request">重命名请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的文件信息</returns>
    [HttpPut("{fileToken}/rename")]
    [ProducesResponseType(typeof(ApiResponse<FileInfoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<FileInfoResponse>>> RenameFile(
        string fileToken,
        [FromBody] RenameFileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _fileService.RenameFileAsync(fileToken, request.NewName, cancellationToken);
            if (result == null)
            {
                return NotFoundResult<FileInfoResponse>("File not found");
            }
            return Success(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequestResult<FileInfoResponse>(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResult<FileInfoResponse>("File not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming file {FileToken}", fileToken);
            return ServerError<FileInfoResponse>("Error renaming file");
        }
    }

    /// <summary>
    /// 获取文件详情
    /// </summary>
    /// <param name="fileToken">文件Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件详细信息</returns>
    [HttpGet("{fileToken}")]
    [ProducesResponseType(typeof(ApiResponse<FileInfoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FileInfoResponse>>> GetFile(
        string fileToken,
        CancellationToken cancellationToken = default)
    {
        var file = await _fileService.GetFileAsync(fileToken, cancellationToken);
        if (file == null)
        {
            return NotFoundResult<FileInfoResponse>("File not found");
        }
        return Success(file);
    }

    /// <summary>
    /// 删除文件（软删除，移入回收站）
    /// </summary>
    /// <param name="fileToken">文件Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>无内容</returns>
    [HttpDelete("{fileToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(
        string fileToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _fileService.DeleteFileAsync(fileToken, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFoundError("File not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileToken}", fileToken);
            return ServerErrorResult("Error deleting file");
        }
    }
}
