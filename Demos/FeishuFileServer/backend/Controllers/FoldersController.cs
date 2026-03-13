using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoldersController : ControllerBase
{
    private readonly IFolderService _folderService;
    private readonly ILogger<FoldersController> _logger;

    public FoldersController(IFolderService folderService, ILogger<FoldersController> logger)
    {
        _folderService = folderService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(FolderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FolderResponse>> CreateFolder(
        [FromBody] FolderCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Folder name is required" });
            }

            var result = await _folderService.CreateFolderAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetFolder), new { folderToken = result.FolderToken }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating folder");
            return StatusCode(500, new { message = "Error creating folder", error = ex.Message });
        }
    }

    [HttpPut("{folderToken}")]
    [ProducesResponseType(typeof(FolderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FolderResponse>> UpdateFolder(
        string folderToken,
        [FromBody] FolderUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _folderService.UpdateFolderAsync(folderToken, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Folder not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating folder {FolderToken}", folderToken);
            return StatusCode(500, new { message = "Error updating folder", error = ex.Message });
        }
    }

    [HttpDelete("{folderToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
            return NotFound(new { message = "Folder not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting folder {FolderToken}", folderToken);
            return StatusCode(500, new { message = "Error deleting folder", error = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(FolderListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FolderListResponse>> GetFolders(
        [FromQuery] string? parentFolderToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _folderService.GetFoldersAsync(parentFolderToken, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{folderToken}")]
    [ProducesResponseType(typeof(FolderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FolderResponse>> GetFolder(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        var folder = await _folderService.GetFolderAsync(folderToken, cancellationToken);
        if (folder == null)
        {
            return NotFound(new { message = "Folder not found" });
        }
        return Ok(folder);
    }

    [HttpGet("{folderToken}/contents")]
    [ProducesResponseType(typeof(FolderContentsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FolderContentsResponse>> GetFolderContents(
        string folderToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _folderService.GetFolderContentsAsync(folderToken, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting folder contents {FolderToken}", folderToken);
            return StatusCode(500, new { message = "Error getting folder contents", error = ex.Message });
        }
    }
}
