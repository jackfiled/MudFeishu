using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 回收站控制器
/// 提供已删除文件和文件夹的查看、恢复、永久删除等功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecycleBinController : FeishuControllerBase
{
    private readonly IRecycleBinService _recycleBinService;
    private readonly ILogger<RecycleBinController> _logger;

    /// <summary>
    /// 初始化回收站控制器
    /// </summary>
    /// <param name="recycleBinService">回收站服务</param>
    /// <param name="logger">日志记录器</param>
    public RecycleBinController(IRecycleBinService recycleBinService, ILogger<RecycleBinController> logger)
    {
        _recycleBinService = recycleBinService;
        _logger = logger;
    }

    /// <summary>
    /// 获取回收站中的文件列表
    /// </summary>
    /// <param name="page">页码，默认1</param>
    /// <param name="pageSize">每页数量，默认20</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已删除的文件列表</returns>
    [HttpGet("files")]
    [ProducesResponseType(typeof(ApiResponse<FileListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FileListResponse>>> GetDeletedFiles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<FileListResponse>("未登录");
        }

        var result = await _recycleBinService.GetDeletedFilesAsync(userId.Value, page, pageSize, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 获取回收站中的文件夹列表
    /// </summary>
    /// <param name="page">页码，默认1</param>
    /// <param name="pageSize">每页数量，默认20</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已删除的文件夹列表</returns>
    [HttpGet("folders")]
    [ProducesResponseType(typeof(ApiResponse<FolderListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FolderListResponse>>> GetDeletedFolders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<FolderListResponse>("未登录");
        }

        var result = await _recycleBinService.GetDeletedFoldersAsync(userId.Value, page, pageSize, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 恢复文件
    /// </summary>
    /// <param name="fileToken">要恢复的文件Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("files/{fileToken}/restore")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> RestoreFile(
        string fileToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _recycleBinService.RestoreFileAsync(fileToken, cancellationToken);
            return Success("文件已恢复");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复文件失败: {FileToken}", fileToken);
            return ServerError("恢复文件失败");
        }
    }

    /// <summary>
    /// 恢复文件夹
    /// </summary>
    /// <param name="folderToken">要恢复的文件夹Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpPost("folders/{folderToken}/restore")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> RestoreFolder(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _recycleBinService.RestoreFolderAsync(folderToken, cancellationToken);
            return Success("文件夹已恢复");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复文件夹失败: {FolderToken}", folderToken);
            return ServerError("恢复文件夹失败");
        }
    }

    /// <summary>
    /// 永久删除文件
    /// </summary>
    /// <param name="fileToken">要永久删除的文件Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>无内容</returns>
    [HttpDelete("files/{fileToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
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
            return NotFoundError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "永久删除文件失败: {FileToken}", fileToken);
            return ServerErrorResult("永久删除文件失败");
        }
    }

    /// <summary>
    /// 永久删除文件夹
    /// </summary>
    /// <param name="folderToken">要永久删除的文件夹Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>无内容</returns>
    [HttpDelete("folders/{folderToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
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
            return NotFoundError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "永久删除文件夹失败: {FolderToken}", folderToken);
            return ServerErrorResult("永久删除文件夹失败");
        }
    }

    /// <summary>
    /// 清空回收站
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    [HttpDelete("empty")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> EmptyRecycleBin(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult("未登录");
        }

        try
        {
            var deletedCount = await _recycleBinService.EmptyRecycleBinAsync(userId.Value, cancellationToken);
            return Success($"已清空回收站，共删除 {deletedCount} 项");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清空回收站失败");
            return ServerError("清空回收站失败");
        }
    }
}
