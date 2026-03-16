using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 文件夹管理控制器
/// 提供文件夹的创建、更新、删除、查询等功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoldersController : FeishuControllerBase
{
    private readonly IFolderService _folderService;
    private readonly ILogger<FoldersController> _logger;

    /// <summary>
    /// 初始化文件夹控制器
    /// </summary>
    /// <param name="folderService">文件夹服务</param>
    /// <param name="logger">日志记录器</param>
    public FoldersController(IFolderService folderService, ILogger<FoldersController> logger)
    {
        _folderService = folderService;
        _logger = logger;
    }

    /// <summary>
    /// 创建文件夹
    /// </summary>
    /// <param name="request">创建文件夹请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的文件夹信息</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FolderResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<FolderResponse>>> CreateFolder(
        [FromBody] FolderCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequestResult<FolderResponse>("Folder name is required");
            }

            var userId = GetCurrentUserId();
            var result = await _folderService.CreateFolderAsync(request, userId, cancellationToken);
            return CreatedAtAction(nameof(GetFolder), new { folderToken = result.FolderToken }, ApiResponse<FolderResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating folder");
            return ServerError<FolderResponse>("Error creating folder");
        }
    }

    /// <summary>
    /// 更新文件夹（重命名或移动）
    /// </summary>
    /// <param name="folderToken">文件夹Token</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的文件夹信息</returns>
    [HttpPut("{folderToken}")]
    [ProducesResponseType(typeof(ApiResponse<FolderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FolderResponse>>> UpdateFolder(
        string folderToken,
        [FromBody] FolderUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _folderService.UpdateFolderAsync(folderToken, request, cancellationToken);
            return Success(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResult<FolderResponse>("Folder not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating folder {FolderToken}", folderToken);
            return ServerError<FolderResponse>("Error updating folder");
        }
    }

    /// <summary>
    /// 删除文件夹（软删除，移入回收站）
    /// </summary>
    /// <param name="folderToken">文件夹Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>无内容</returns>
    [HttpDelete("{folderToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
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
            return NotFoundError("Folder not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting folder {FolderToken}", folderToken);
            return ServerErrorResult("Error deleting folder");
        }
    }

    /// <summary>
    /// 获取文件夹列表
    /// </summary>
    /// <param name="parentFolderToken">父文件夹Token（可选，不传则获取根目录文件夹）</param>
    /// <param name="page">页码，默认1</param>
    /// <param name="pageSize">每页数量，默认50</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹列表响应</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<FolderListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FolderListResponse>>> GetFolders(
        [FromQuery] string? parentFolderToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _folderService.GetFoldersAsync(parentFolderToken, userId, page, pageSize, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// 获取文件夹详情
    /// </summary>
    /// <param name="folderToken">文件夹Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹详细信息</returns>
    [HttpGet("{folderToken}")]
    [ProducesResponseType(typeof(ApiResponse<FolderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FolderResponse>>> GetFolder(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        var folder = await _folderService.GetFolderAsync(folderToken, cancellationToken);
        if (folder == null)
        {
            return NotFoundResult<FolderResponse>("Folder not found");
        }
        return Success(folder);
    }

    /// <summary>
    /// 获取文件夹内容（包含子文件夹和文件）
    /// </summary>
    /// <param name="folderToken">文件夹Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹内容响应</returns>
    [HttpGet("{folderToken}/contents")]
    [ProducesResponseType(typeof(ApiResponse<FolderContentsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FolderContentsResponse>>> GetFolderContents(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _folderService.GetFolderContentsAsync(folderToken, cancellationToken);
            return Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting folder contents {FolderToken}", folderToken);
            return ServerError<FolderContentsResponse>("Error getting folder contents");
        }
    }
}
