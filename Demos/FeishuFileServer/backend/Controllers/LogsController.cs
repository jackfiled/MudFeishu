using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LogsController : FeishuControllerBase
{
    private readonly IOperationLogService _logService;
    private readonly ILogger<LogsController> _logger;

    public LogsController(IOperationLogService logService, ILogger<LogsController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    [HttpGet("user")]
    [ProducesResponseType(typeof(ApiResponse<UserLogsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserLogsResponse>>> GetUserLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<UserLogsResponse>("未登录");
        }

        var logs = await _logService.GetUserLogsAsync(userId.Value, page, pageSize, cancellationToken);
        return Success(new UserLogsResponse { Logs = logs, TotalCount = logs.Count });
    }

    [HttpGet("resource/{resourceToken}")]
    [ProducesResponseType(typeof(ApiResponse<UserLogsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserLogsResponse>>> GetResourceLogs(
        string resourceToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var logs = await _logService.GetResourceLogsAsync(resourceToken, page, pageSize, cancellationToken);
        return Success(new UserLogsResponse { Logs = logs, TotalCount = logs.Count });
    }
}

public class UserLogsResponse
{
    public List<FeishuFileServer.Models.OperationLog> Logs { get; set; } = new();
    public int TotalCount { get; set; }
}
