using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

[ApiController]
[Route("api/files/{fileToken}/versions")]
[Authorize]
public class VersionsController : FeishuControllerBase
{
    private readonly IVersionService _versionService;
    private readonly ILogger<VersionsController> _logger;

    public VersionsController(
        IVersionService versionService,
        ILogger<VersionsController> logger)
    {
        _versionService = versionService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<VersionListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VersionListResponse>>> GetVersions(
        [FromRoute] string fileToken,
        CancellationToken cancellationToken)
    {
        var versions = await _versionService.GetVersionsAsync(fileToken, cancellationToken);

        return Success(new VersionListResponse
        {
            Versions = versions,
            TotalCount = versions.Count
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VersionCreateResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<VersionCreateResponse>>> CreateVersion(
        [FromRoute] string fileToken,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequestResult<VersionCreateResponse>("File is required");
        }

        var result = await _versionService.CreateVersionAsync(fileToken, file, cancellationToken);
        return CreatedAtAction(nameof(DownloadVersion), new { fileToken, versionToken = result.VersionToken }, ApiResponse<VersionCreateResponse>.Ok(result));
    }

    [HttpGet("{versionToken}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> DownloadVersion(
        [FromRoute] string fileToken,
        [FromRoute] string versionToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await _versionService.DownloadVersionAsync(fileToken, versionToken, cancellationToken);
            return File(content, "application/octet-stream", $"version_{versionToken}");
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, ApiResponse.Fail("Version download is not yet implemented", 501));
        }
        catch (KeyNotFoundException)
        {
            return NotFoundError("Version not found");
        }
    }

    [HttpPut("{versionToken}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreVersion(
        [FromRoute] string fileToken,
        [FromRoute] string versionToken,
        CancellationToken cancellationToken)
    {
        await _versionService.RestoreVersionAsync(fileToken, versionToken, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{versionToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVersion(
        [FromRoute] string fileToken,
        [FromRoute] string versionToken,
        CancellationToken cancellationToken)
    {
        await _versionService.DeleteVersionAsync(fileToken, versionToken, cancellationToken);
        return NoContent();
    }
}
