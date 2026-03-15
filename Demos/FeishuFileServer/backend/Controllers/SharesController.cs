using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SharesController : FeishuControllerBase
{
    private readonly IShareService _shareService;
    private readonly ILogger<SharesController> _logger;

    public SharesController(IShareService shareService, ILogger<SharesController> logger)
    {
        _shareService = shareService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ShareResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ShareResponse>>> CreateShare(
        [FromBody] CreateShareRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<ShareResponse>("未登录");
        }

        try
        {
            var result = await _shareService.CreateShareAsync(request, userId.Value, cancellationToken);
            return CreatedAtAction(nameof(GetShare), new { shareCode = result.ShareCode }, ApiResponse<ShareResponse>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult<ShareResponse>(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequestResult<ShareResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建分享失败");
            return ServerError<ShareResponse>("创建分享失败");
        }
    }

    [HttpGet("{shareCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ShareContentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ShareContentResponse>>> AccessShare(
        string shareCode,
        [FromQuery] string? password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _shareService.AccessShareAsync(shareCode, password, cancellationToken);
            return Success(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult<ShareContentResponse>(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedResult<ShareContentResponse>(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult<ShareContentResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "访问分享失败: {ShareCode}", shareCode);
            return ServerError<ShareContentResponse>("访问分享失败");
        }
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ShareListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ShareListResponse>>> GetMyShares(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<ShareListResponse>("未登录");
        }

        var result = await _shareService.GetUserSharesAsync(userId.Value, page, pageSize, cancellationToken);
        return Success(result);
    }

    [HttpGet("{shareCode}/info")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ShareResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ShareResponse>>> GetShare(
        string shareCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _shareService.AccessShareAsync(shareCode, null, cancellationToken);
            return Success(new ShareResponse
            {
                ResourceType = result.ResourceType,
                ResourceName = result.ResourceName,
                AllowDownload = result.AllowDownload
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult<ShareResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分享信息失败: {ShareCode}", shareCode);
            return ServerError<ShareResponse>("获取分享信息失败");
        }
    }

    [HttpGet("{shareCode}/download")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DownloadSharedFile(
        string shareCode,
        [FromQuery] string? password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var content = await _shareService.DownloadSharedFileAsync(shareCode, password, cancellationToken);
            
            var shareContent = await _shareService.AccessShareAsync(shareCode, password, cancellationToken);
            var fileName = shareContent.ResourceName;

            return File(content, "application/octet-stream", fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载分享文件失败: {ShareCode}", shareCode);
            return ServerErrorResult("下载分享文件失败");
        }
    }

    [HttpPut("{shareId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ShareResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ShareResponse>>> UpdateShare(
        long shareId,
        [FromBody] UpdateShareRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<ShareResponse>("未登录");
        }

        try
        {
            var result = await _shareService.UpdateShareAsync(shareId, request, userId.Value, cancellationToken);
            return Success(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult<ShareResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新分享失败: {ShareId}", shareId);
            return ServerError<ShareResponse>("更新分享失败");
        }
    }

    [HttpDelete("{shareId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteShare(
        long shareId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedError("未登录");
        }

        try
        {
            await _shareService.DeleteShareAsync(shareId, userId.Value, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除分享失败: {ShareId}", shareId);
            return ServerErrorResult("删除分享失败");
        }
    }
}
