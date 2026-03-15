using System.Security.Claims;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeishuFileServer.Controllers;

[ApiController]
[Route("api/sync")]
[Authorize]
public class SyncController : FeishuControllerBase
{
    private readonly IFeishuSyncService _syncService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(IFeishuSyncService syncService, ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    [HttpPost("all")]
    [ProducesResponseType(typeof(ApiResponse<SyncResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<SyncResult>>> SyncAll(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<SyncResult>("未登录");
        }

        try
        {
            var result = await _syncService.SyncAllAsync(userId.Value, cancellationToken);
            return Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步失败");
            return ServerError<SyncResult>("同步失败");
        }
    }

    [HttpPost("folder/{folderToken}")]
    [ProducesResponseType(typeof(ApiResponse<SyncResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<SyncResult>>> SyncFolder(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<SyncResult>("未登录");
        }

        try
        {
            var result = await _syncService.SyncFolderAsync(folderToken, userId.Value, cancellationToken);
            return Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步文件夹失败");
            return ServerError<SyncResult>("同步文件夹失败");
        }
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<SyncStatus>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<SyncStatus>>> GetStatus(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<SyncStatus>("未登录");
        }

        try
        {
            var status = await _syncService.GetSyncStatusAsync(userId.Value, cancellationToken);
            return Success(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取同步状态失败");
            return ServerError<SyncStatus>("获取同步状态失败");
        }
    }
}
