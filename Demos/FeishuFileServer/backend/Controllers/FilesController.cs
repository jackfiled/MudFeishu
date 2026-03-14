// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 文件控制器
/// 提供文件的上传、下载、查询、删除等API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;

    /// <summary>
    /// 初始化文件控制器实例
    /// </summary>
    /// <param name="fileService">文件服务</param>
    /// <param name="logger">日志记录器</param>
    public FilesController(IFileService fileService, ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>
    /// 从当前请求的JWT令牌中获取用户ID
    /// </summary>
    /// <returns>用户ID，未登录时返回null</returns>
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// 上传文件
    /// <para>将文件上传到飞书云存储，支持指定目标文件夹</para>
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <param name="folderToken">目标文件夹令牌，为空表示根目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上传响应，包含文件令牌和基本信息</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileUploadResponse>> UploadFile(
        IFormFile file,
        [FromForm] string? folderToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _fileService.UploadFileAsync(file, folderToken, userId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, new { message = "Error uploading file", error = ex.Message });
        }
    }

    /// <summary>
    /// 下载文件
    /// <para>从飞书云存储下载指定文件，支持下载特定版本</para>
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="versionToken">版本令牌，可选，指定时下载特定版本</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容</returns>
    [HttpGet("{fileToken}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
                return NotFound(new { message = "File not found" });
            }

            var content = await _fileService.DownloadFileAsync(fileToken, versionToken, cancellationToken);

            var mimeType = fileInfo.MimeType ?? "application/octet-stream";
            var fileName = Uri.EscapeDataString(fileInfo.FileName);

            return File(content, mimeType, fileInfo.FileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "File not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileToken}", fileToken);
            return StatusCode(500, new { message = "Error downloading file", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取文件列表
    /// <para>支持按文件夹筛选和搜索，支持分页</para>
    /// </summary>
    /// <param name="folderToken">文件夹令牌，可选</param>
    /// <param name="search">搜索关键词，可选，按文件名模糊搜索</param>
    /// <param name="page">页码，从1开始</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件列表响应</returns>
    [HttpGet]
    [ProducesResponseType(typeof(FileListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FileListResponse>> GetFiles(
        [FromQuery] string? folderToken,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _fileService.GetFilesAsync(folderToken, userId, search, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 重命名文件
    /// <para>更新文件的显示名称</para>
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="request">重命名请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的文件信息</returns>
    [HttpPut("{fileToken}/rename")]
    [ProducesResponseType(typeof(FileInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileInfoResponse>> RenameFile(
        string fileToken,
        [FromBody] RenameFileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _fileService.RenameFileAsync(fileToken, request.NewName, cancellationToken);
            if (result == null)
            {
                return NotFound(new { message = "File not found" });
            }
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "File not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming file {FileToken}", fileToken);
            return StatusCode(500, new { message = "Error renaming file", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件详细信息</returns>
    [HttpGet("{fileToken}")]
    [ProducesResponseType(typeof(FileInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileInfoResponse>> GetFile(
        string fileToken,
        CancellationToken cancellationToken = default)
    {
        var file = await _fileService.GetFileAsync(fileToken, cancellationToken);
        if (file == null)
        {
            return NotFound(new { message = "File not found" });
        }
        return Ok(file);
    }

    /// <summary>
    /// 删除文件
    /// <para>从飞书云存储和本地数据库删除文件</para>
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{fileToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
            return NotFound(new { message = "File not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileToken}", fileToken);
            return StatusCode(500, new { message = "Error deleting file", error = ex.Message });
        }
    }
}
