using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecycleBinController : FeishuControllerBase
{
    private readonly IRecycleBinService _recycleBinService;
    private readonly ILogger<RecycleBinController> _logger;

    public RecycleBinController(IRecycleBinService recycleBinService, ILogger<RecycleBinController> logger)
    {
        _recycleBinService = recycleBinService;
        _logger = logger;
    }

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
