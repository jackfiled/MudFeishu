using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 文件夹控制器
/// 提供文件夹的创建、更新、删除、查询等API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoldersController : ControllerBase
{
    private readonly IFolderService _folderService;
    private readonly ILogger<FoldersController> _logger;

    /// <summary>
    /// 初始化文件夹控制器实例
    /// </summary>
    /// <param name="folderService">文件夹服务</param>
    /// <param name="logger">日志记录器</param>
    public FoldersController(IFolderService folderService, ILogger<FoldersController> logger)
    {
        _folderService = folderService;
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
    /// 创建文件夹
    /// <para>在飞书云盘创建文件夹，并在本地数据库创建记录</para>
    /// </summary>
    /// <param name="request">创建文件夹请求，包含文件夹名称和父文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的文件夹信息</returns>
    [HttpPost]
    [ProducesResponseType(typeof(FolderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FolderResponse>> CreateFolder(
        [FromBody] FolderCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Folder name is required" });
            }

            var userId = GetCurrentUserId();
            var result = await _folderService.CreateFolderAsync(request, userId, cancellationToken);
            return CreatedAtAction(nameof(GetFolder), new { folderToken = result.FolderToken }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating folder");
            return StatusCode(500, new { message = "Error creating folder", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新文件夹
    /// <para>更新文件夹名称</para>
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="request">更新文件夹请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的文件夹信息</returns>
    [HttpPut("{folderToken}")]
    [ProducesResponseType(typeof(FolderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FolderResponse>> UpdateFolder(
        string folderToken,
        [FromBody] FolderUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _folderService.UpdateFolderAsync(folderToken, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Folder not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating folder {FolderToken}", folderToken);
            return StatusCode(500, new { message = "Error updating folder", error = ex.Message });
        }
    }

    /// <summary>
    /// 删除文件夹
    /// <para>从飞书云盘和本地数据库删除文件夹，同时标记子文件夹和文件为已删除</para>
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{folderToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFolder(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _folderService.DeleteFolderAsync(folderToken, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Folder not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting folder {FolderToken}", folderToken);
            return StatusCode(500, new { message = "Error deleting folder", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取文件夹列表
    /// <para>支持按父文件夹筛选，支持分页</para>
    /// </summary>
    /// <param name="parentFolderToken">父文件夹令牌，可选</param>
    /// <param name="page">页码，从1开始</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹列表响应</returns>
    [HttpGet]
    [ProducesResponseType(typeof(FolderListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FolderListResponse>> GetFolders(
        [FromQuery] string? parentFolderToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _folderService.GetFoldersAsync(parentFolderToken, userId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 获取文件夹信息
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹详细信息</returns>
    [HttpGet("{folderToken}")]
    [ProducesResponseType(typeof(FolderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FolderResponse>> GetFolder(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        var folder = await _folderService.GetFolderAsync(folderToken, cancellationToken);
        if (folder == null)
        {
            return NotFound(new { message = "Folder not found" });
        }
        return Ok(folder);
    }

    /// <summary>
    /// 获取文件夹内容
    /// <para>获取文件夹内的子文件夹和文件列表</para>
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹内容响应</returns>
    [HttpGet("{folderToken}/contents")]
    [ProducesResponseType(typeof(FolderContentsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FolderContentsResponse>> GetFolderContents(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _folderService.GetFolderContentsAsync(folderToken, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting folder contents {FolderToken}", folderToken);
            return StatusCode(500, new { message = "Error getting folder contents", error = ex.Message });
        }
    }
}
