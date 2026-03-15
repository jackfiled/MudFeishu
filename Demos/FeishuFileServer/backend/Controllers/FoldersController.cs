using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoldersController : FeishuControllerBase
{
    private readonly IFolderService _folderService;
    private readonly ILogger<FoldersController> _logger;

    public FoldersController(IFolderService folderService, ILogger<FoldersController> logger)
    {
        _folderService = folderService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FolderResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<FolderResponse>>> CreateFolder(
        [FromBody] FolderCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequestResult<FolderResponse>("Folder name is required");
            }

            var userId = GetCurrentUserId();
            var result = await _folderService.CreateFolderAsync(request, userId, cancellationToken);
            return CreatedAtAction(nameof(GetFolder), new { folderToken = result.FolderToken }, ApiResponse<FolderResponse>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating folder");
            return ServerError<FolderResponse>("Error creating folder");
        }
    }

    [HttpPut("{folderToken}")]
    [ProducesResponseType(typeof(ApiResponse<FolderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FolderResponse>>> UpdateFolder(
        string folderToken,
        [FromBody] FolderUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _folderService.UpdateFolderAsync(folderToken, request, cancellationToken);
            return Success(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResult<FolderResponse>("Folder not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating folder {FolderToken}", folderToken);
            return ServerError<FolderResponse>("Error updating folder");
        }
    }

    [HttpDelete("{folderToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFolder(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _folderService.DeleteFolderAsync(folderToken, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFoundError("Folder not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting folder {FolderToken}", folderToken);
            return ServerErrorResult("Error deleting folder");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<FolderListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FolderListResponse>>> GetFolders(
        [FromQuery] string? parentFolderToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _folderService.GetFoldersAsync(parentFolderToken, userId, page, pageSize, cancellationToken);
        return Success(result);
    }

    [HttpGet("{folderToken}")]
    [ProducesResponseType(typeof(ApiResponse<FolderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FolderResponse>>> GetFolder(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        var folder = await _folderService.GetFolderAsync(folderToken, cancellationToken);
        if (folder == null)
        {
            return NotFoundResult<FolderResponse>("Folder not found");
        }
        return Success(folder);
    }

    [HttpGet("{folderToken}/contents")]
    [ProducesResponseType(typeof(ApiResponse<FolderContentsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FolderContentsResponse>>> GetFolderContents(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _folderService.GetFolderContentsAsync(folderToken, cancellationToken);
            return Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting folder contents {FolderToken}", folderToken);
            return ServerError<FolderContentsResponse>("Error getting folder contents");
        }
    }
}
