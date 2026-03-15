using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

[ApiController]
[Route("api/files/chunk")]
[Authorize]
public class ChunkUploadController : FeishuControllerBase
{
    private readonly IChunkUploadService _chunkUploadService;
    private readonly ILogger<ChunkUploadController> _logger;

    public ChunkUploadController(IChunkUploadService chunkUploadService, ILogger<ChunkUploadController> logger)
    {
        _chunkUploadService = chunkUploadService;
        _logger = logger;
    }

    [HttpPost("init")]
    [ProducesResponseType(typeof(ApiResponse<InitChunkUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<InitChunkUploadResponse>>> InitUpload(
        [FromBody] InitChunkUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<InitChunkUploadResponse>("未登录");
        }

        try
        {
            var result = await _chunkUploadService.InitUploadAsync(request, userId.Value, cancellationToken);
            return Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化分片上传失败");
            return ServerError<InitChunkUploadResponse>("初始化分片上传失败");
        }
    }

    [HttpPost("{uploadId}/{chunkNumber}")]
    [ProducesResponseType(typeof(ApiResponse<ChunkUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [RequestSizeLimit(100_000_000)]
    public async Task<ActionResult<ApiResponse<ChunkUploadResponse>>> UploadChunk(
        string uploadId,
        int chunkNumber,
        IFormFile chunk,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<ChunkUploadResponse>("未登录");
        }

        try
        {
            await using var stream = chunk.OpenReadStream();
            var result = await _chunkUploadService.UploadChunkAsync(uploadId, chunkNumber, stream, userId.Value, cancellationToken);
            return Success(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult<ChunkUploadResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传分片失败");
            return ServerError<ChunkUploadResponse>("上传分片失败");
        }
    }

    [HttpPost("{uploadId}/complete")]
    [ProducesResponseType(typeof(ApiResponse<ChunkUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ChunkUploadResponse>>> CompleteUpload(
        string uploadId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<ChunkUploadResponse>("未登录");
        }

        try
        {
            var result = await _chunkUploadService.CompleteUploadAsync(uploadId, userId.Value, cancellationToken);
            return Success(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult<ChunkUploadResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成分片上传失败");
            return ServerError<ChunkUploadResponse>("完成分片上传失败");
        }
    }

    [HttpDelete("{uploadId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> CancelUpload(
        string uploadId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult("未登录");
        }

        try
        {
            await _chunkUploadService.CancelUploadAsync(uploadId, userId.Value, cancellationToken);
            return Success("已取消上传");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消分片上传失败");
            return ServerError("取消分片上传失败");
        }
    }

    [HttpGet("{uploadId}/progress")]
    [ProducesResponseType(typeof(ApiResponse<ChunkUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ChunkUploadResponse>>> GetProgress(
        string uploadId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<ChunkUploadResponse>("未登录");
        }

        try
        {
            var result = await _chunkUploadService.GetProgressAsync(uploadId, userId.Value, cancellationToken);
            if (result == null)
            {
                return NotFoundResult<ChunkUploadResponse>("上传任务不存在");
            }
            return Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取上传进度失败");
            return ServerError<ChunkUploadResponse>("获取上传进度失败");
        }
    }
}
