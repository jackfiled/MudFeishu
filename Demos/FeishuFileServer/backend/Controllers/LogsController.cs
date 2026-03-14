using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 操作日志控制器
/// 提供操作日志查询功能的API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly IOperationLogService _logService;
    private readonly ILogger<LogsController> _logger;

    /// <summary>
    /// 初始化操作日志控制器实例
    /// </summary>
    /// <param name="logService">操作日志服务</param>
    /// <param name="logger">日志记录器</param>
    public LogsController(IOperationLogService logService, ILogger<LogsController> logger)
    {
        _logService = logService;
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
    /// 获取当前用户的操作日志
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作日志列表</returns>
    [HttpGet("user")]
    [ProducesResponseType(typeof(List<Models.OperationLog>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        var logs = await _logService.GetUserLogsAsync(userId.Value, page, pageSize, cancellationToken);
        return Ok(new { logs, totalCount = logs.Count });
    }

    /// <summary>
    /// 获取指定资源的操作日志
    /// </summary>
    /// <param name="resourceToken">资源令牌</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作日志列表</returns>
    [HttpGet("resource/{resourceToken}")]
    [ProducesResponseType(typeof(List<Models.OperationLog>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResourceLogs(
        string resourceToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var logs = await _logService.GetResourceLogsAsync(resourceToken, page, pageSize, cancellationToken);
        return Ok(new { logs, totalCount = logs.Count });
    }
}
