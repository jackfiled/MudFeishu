using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 批量操作控制器
/// 提供批量删除、移动、复制功能的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BatchController : ControllerBase
{
    private readonly IBatchService _batchService;
    private readonly ILogger<BatchController> _logger;

    /// <summary>
    /// 初始化批量操作控制器实例
    /// </summary>
    public BatchController(IBatchService batchService, ILogger<BatchController> logger)
    {
        _batchService = batchService;
        _logger = logger;
    }

    /// <summary>
    /// 从当前请求的JWT令牌中获取用户ID
    /// </summary>
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
    /// 批量删除文件和文件夹
    /// </summary>
    /// <param name="request">批量删除请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量操作响应</returns>
    [HttpPost("delete")]
    [ProducesResponseType(typeof(BatchOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BatchOperationResponse>> BatchDelete(
        [FromBody] BatchDeleteRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            var result = await _batchService.BatchDeleteAsync(request, userId.Value, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除失败");
            return StatusCode(500, new { message = "批量删除失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 批量移动文件和文件夹
    /// </summary>
    /// <param name="request">批量移动请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量操作响应</returns>
    [HttpPost("move")]
    [ProducesResponseType(typeof(BatchOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BatchOperationResponse>> BatchMove(
        [FromBody] BatchMoveRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            var result = await _batchService.BatchMoveAsync(request, userId.Value, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量移动失败");
            return StatusCode(500, new { message = "批量移动失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 批量复制文件和文件夹
    /// </summary>
    /// <param name="request">批量复制请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量操作响应</returns>
    [HttpPost("copy")]
    [ProducesResponseType(typeof(BatchOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BatchOperationResponse>> BatchCopy(
        [FromBody] BatchCopyRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            var result = await _batchService.BatchCopyAsync(request, userId.Value, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量复制失败");
            return StatusCode(500, new { message = "批量复制失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 批量恢复文件和文件夹
    /// </summary>
    /// <param name="request">批量恢复请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量操作响应</returns>
    [HttpPost("restore")]
    [ProducesResponseType(typeof(BatchOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BatchOperationResponse>> BatchRestore(
        [FromBody] BatchRestoreRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            var result = await _batchService.BatchRestoreAsync(request, userId.Value, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量恢复失败");
            return StatusCode(500, new { message = "批量恢复失败", error = ex.Message });
        }
    }
}
