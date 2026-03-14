using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 回收站控制器
/// 提供回收站文件和文件夹的管理功能API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecycleBinController : ControllerBase
{
    private readonly IRecycleBinService _recycleBinService;
    private readonly ILogger<RecycleBinController> _logger;

    /// <summary>
    /// 初始化回收站控制器实例
    /// </summary>
    /// <param name="recycleBinService">回收站服务</param>
    /// <param name="logger">日志记录器</param>
    public RecycleBinController(IRecycleBinService recycleBinService, ILogger<RecycleBinController> logger)
    {
        _recycleBinService = recycleBinService;
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
    /// 获取回收站中的文件列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件列表响应</returns>
    [HttpGet("files")]
    [ProducesResponseType(typeof(FileListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FileListResponse>> GetDeletedFiles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        var result = await _recycleBinService.GetDeletedFilesAsync(userId.Value, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 获取回收站中的文件夹列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹列表响应</returns>
    [HttpGet("folders")]
    [ProducesResponseType(typeof(FolderListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FolderListResponse>> GetDeletedFolders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        var result = await _recycleBinService.GetDeletedFoldersAsync(userId.Value, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 恢复文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>恢复结果</returns>
    [HttpPost("files/{fileToken}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreFile(
        string fileToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _recycleBinService.RestoreFileAsync(fileToken, cancellationToken);
            return Ok(new { message = "文件已恢复" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复文件失败: {FileToken}", fileToken);
            return StatusCode(500, new { message = "恢复文件失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 恢复文件夹
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>恢复结果</returns>
    [HttpPost("folders/{folderToken}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreFolder(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _recycleBinService.RestoreFolderAsync(folderToken, cancellationToken);
            return Ok(new { message = "文件夹已恢复" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复文件夹失败: {FolderToken}", folderToken);
            return StatusCode(500, new { message = "恢复文件夹失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 永久删除文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    [HttpDelete("files/{fileToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PermanentlyDeleteFile(
        string fileToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _recycleBinService.PermanentlyDeleteFileAsync(fileToken, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "永久删除文件失败: {FileToken}", fileToken);
            return StatusCode(500, new { message = "永久删除文件失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 永久删除文件夹
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    [HttpDelete("folders/{folderToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PermanentlyDeleteFolder(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _recycleBinService.PermanentlyDeleteFolderAsync(folderToken, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "永久删除文件夹失败: {FolderToken}", folderToken);
            return StatusCode(500, new { message = "永久删除文件夹失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 清空回收站
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>清空结果</returns>
    [HttpDelete("empty")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> EmptyRecycleBin(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            var deletedCount = await _recycleBinService.EmptyRecycleBinAsync(userId.Value, cancellationToken);
            return Ok(new { message = $"已清空回收站，共删除 {deletedCount} 项", deletedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空回收站失败");
            return StatusCode(500, new { message = "清空回收站失败", error = ex.Message });
        }
    }
}
