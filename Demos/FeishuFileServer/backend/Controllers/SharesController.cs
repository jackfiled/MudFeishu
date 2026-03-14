using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 分享控制器
/// 提供文件和文件夹分享功能的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SharesController : ControllerBase
{
    private readonly IShareService _shareService;
    private readonly ILogger<SharesController> _logger;

    /// <summary>
    /// 初始化分享控制器实例
    /// </summary>
    /// <param name="shareService">分享服务</param>
    /// <param name="logger">日志记录器</param>
    public SharesController(IShareService shareService, ILogger<SharesController> logger)
    {
        _shareService = shareService;
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
    /// 创建分享链接
    /// <para>为文件或文件夹创建分享链接</para>
    /// </summary>
    /// <param name="request">创建分享请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分享响应</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ShareResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShareResponse>> CreateShare(
        [FromBody] CreateShareRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            var result = await _shareService.CreateShareAsync(request, userId.Value, cancellationToken);
            return CreatedAtAction(nameof(GetShare), new { shareCode = result.ShareCode }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建分享失败");
            return StatusCode(500, new { message = "创建分享失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 访问分享内容
    /// <para>通过分享码访问分享的文件或文件夹</para>
    /// </summary>
    /// <param name="shareCode">分享码</param>
    /// <param name="password">访问密码（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分享内容响应</returns>
    [HttpGet("{shareCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ShareContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShareContentResponse>> AccessShare(
        string shareCode,
        [FromQuery] string? password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _shareService.AccessShareAsync(shareCode, password, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "访问分享失败: {ShareCode}", shareCode);
            return StatusCode(500, new { message = "访问分享失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取用户的分享列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分享列表响应</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ShareListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShareListResponse>> GetMyShares(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        var result = await _shareService.GetUserSharesAsync(userId.Value, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 获取分享信息
    /// </summary>
    /// <param name="shareCode">分享码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分享响应</returns>
    [HttpGet("{shareCode}/info")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ShareResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShareResponse>> GetShare(
        string shareCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _shareService.AccessShareAsync(shareCode, null, cancellationToken);
            return Ok(new ShareResponse
            {
                ResourceType = result.ResourceType,
                ResourceName = result.ResourceName,
                AllowDownload = result.AllowDownload
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分享信息失败: {ShareCode}", shareCode);
            return StatusCode(500, new { message = "获取分享信息失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 下载分享的文件
    /// </summary>
    /// <param name="shareCode">分享码</param>
    /// <param name="password">访问密码（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容</returns>
    [HttpGet("{shareCode}/download")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载分享文件失败: {ShareCode}", shareCode);
            return StatusCode(500, new { message = "下载分享文件失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新分享设置
    /// </summary>
    /// <param name="shareId">分享ID</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的分享响应</returns>
    [HttpPut("{shareId}")]
    [Authorize]
    [ProducesResponseType(typeof(ShareResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShareResponse>> UpdateShare(
        long shareId,
        [FromBody] UpdateShareRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            var result = await _shareService.UpdateShareAsync(shareId, request, userId.Value, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新分享失败: {ShareId}", shareId);
            return StatusCode(500, new { message = "更新分享失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 删除分享
    /// </summary>
    /// <param name="shareId">分享ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{shareId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteShare(
        long shareId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            await _shareService.DeleteShareAsync(shareId, userId.Value, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除分享失败: {ShareId}", shareId);
            return StatusCode(500, new { message = "删除分享失败", error = ex.Message });
        }
    }
}
