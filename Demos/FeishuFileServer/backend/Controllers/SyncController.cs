using System.Security.Claims;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 同步控制器
/// 提供飞书云盘数据同步功能
/// </summary>
[ApiController]
[Route("api/sync")]
[Authorize]
public class SyncController : FeishuControllerBase
{
    private readonly IFeishuSyncService _syncService;
    private readonly ILogger<SyncController> _logger;

    /// <summary>
    /// 初始化同步控制器
    /// </summary>
    /// <param name="syncService">同步服务</param>
    /// <param name="logger">日志记录器</param>
    public SyncController(IFeishuSyncService syncService, ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    /// <summary>
    /// 同步所有文件和文件夹
    /// 从飞书云盘同步所有数据到本地数据库
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步结果，包含同步的文件和文件夹数量</returns>
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

    /// <summary>
    /// 同步指定文件夹
    /// 从飞书云盘同步指定文件夹及其子内容到本地数据库
    /// </summary>
    /// <param name="folderToken">文件夹Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步结果，包含同步的文件和文件夹数量</returns>
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

    /// <summary>
    /// 获取同步状态
    /// 返回最近一次同步的状态信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步状态信息</returns>
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
