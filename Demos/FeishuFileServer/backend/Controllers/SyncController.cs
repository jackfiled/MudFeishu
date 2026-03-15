using System.Security.Claims;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 飞书云空间同步控制器
/// </summary>
[ApiController]
[Route("api/sync")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly IFeishuSyncService _syncService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(IFeishuSyncService syncService, ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

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
    /// 同步所有飞书云空间数据
    /// </summary>
    [HttpPost("all")]
    [ProducesResponseType(typeof(SyncResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SyncResult>> SyncAll(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            var result = await _syncService.SyncAllAsync(userId.Value, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步失败");
            return StatusCode(500, new { message = "同步失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 同步指定文件夹
    /// </summary>
    [HttpPost("folder/{folderToken}")]
    [ProducesResponseType(typeof(SyncResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SyncResult>> SyncFolder(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            var result = await _syncService.SyncFolderAsync(folderToken, userId.Value, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步文件夹失败");
            return StatusCode(500, new { message = "同步文件夹失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取同步状态
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SyncStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SyncStatus>> GetStatus(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            var status = await _syncService.GetSyncStatusAsync(userId.Value, cancellationToken);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取同步状态失败");
            return StatusCode(500, new { message = "获取同步状态失败", error = ex.Message });
        }
    }
}
