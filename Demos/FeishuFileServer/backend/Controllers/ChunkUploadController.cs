using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 分片上传控制器
/// </summary>
[ApiController]
[Route("api/files/chunk")]
[Authorize]
public class ChunkUploadController : ControllerBase
{
    private readonly IChunkUploadService _chunkUploadService;
    private readonly ILogger<ChunkUploadController> _logger;

    public ChunkUploadController(IChunkUploadService chunkUploadService, ILogger<ChunkUploadController> logger)
    {
        _chunkUploadService = chunkUploadService;
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
    /// 初始化分片上传
    /// </summary>
    [HttpPost("init")]
    [ProducesResponseType(typeof(InitChunkUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<InitChunkUploadResponse>> InitUpload(
        [FromBody] InitChunkUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            var result = await _chunkUploadService.InitUploadAsync(request, userId.Value, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化分片上传失败");
            return StatusCode(500, new { message = "初始化分片上传失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 上传分片
    /// </summary>
    [HttpPost("{uploadId}/{chunkNumber}")]
    [ProducesResponseType(typeof(ChunkUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequestSizeLimit(100_000_000)]
    public async Task<ActionResult<ChunkUploadResponse>> UploadChunk(
        string uploadId,
        int chunkNumber,
        IFormFile chunk,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            await using var stream = chunk.OpenReadStream();
            var result = await _chunkUploadService.UploadChunkAsync(uploadId, chunkNumber, stream, userId.Value, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传分片失败");
            return StatusCode(500, new { message = "上传分片失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 完成分片上传
    /// </summary>
    [HttpPost("{uploadId}/complete")]
    [ProducesResponseType(typeof(ChunkUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ChunkUploadResponse>> CompleteUpload(
        string uploadId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            var result = await _chunkUploadService.CompleteUploadAsync(uploadId, userId.Value, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成分片上传失败");
            return StatusCode(500, new { message = "完成分片上传失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 取消分片上传
    /// </summary>
    [HttpDelete("{uploadId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> CancelUpload(
        string uploadId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            await _chunkUploadService.CancelUploadAsync(uploadId, userId.Value, cancellationToken);
            return Ok(new { message = "已取消上传" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消分片上传失败");
            return StatusCode(500, new { message = "取消分片上传失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取上传进度
    /// </summary>
    [HttpGet("{uploadId}/progress")]
    [ProducesResponseType(typeof(ChunkUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChunkUploadResponse>> GetProgress(
        string uploadId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            var result = await _chunkUploadService.GetProgressAsync(uploadId, userId.Value, cancellationToken);
            if (result == null)
            {
                return NotFound(new { message = "上传任务不存在" });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取上传进度失败");
            return StatusCode(500, new { message = "获取上传进度失败", error = ex.Message });
        }
    }
}
