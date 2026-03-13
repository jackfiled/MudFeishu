using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

[ApiController]
[Route("api/files/{fileToken}/versions")]
[Authorize]
public class VersionsController : ControllerBase
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
    public async Task<ActionResult<VersionListResponse>> GetVersions(
        [FromRoute] string fileToken,
        CancellationToken cancellationToken)
    {
        var versions = await _versionService.GetVersionsAsync(fileToken, cancellationToken);

        return Ok(new VersionListResponse
        {
            Versions = versions,
            TotalCount = versions.Count
        });
    }

    [HttpPost]
    public async Task<ActionResult<VersionCreateResponse>> CreateVersion(
        [FromRoute] string fileToken,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is required");
        }

        var result = await _versionService.CreateVersionAsync(fileToken, file, cancellationToken);
        return CreatedAtAction(nameof(DownloadVersion), new { fileToken, versionToken = result.VersionToken }, result);
    }

    [HttpGet("{versionToken}/download")]
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
            return StatusCode(501, "Version download is not yet implemented");
        }
    }

    [HttpPut("{versionToken}/restore")]
    public async Task<IActionResult> RestoreVersion(
        [FromRoute] string fileToken,
        [FromRoute] string versionToken,
        CancellationToken cancellationToken)
    {
        await _versionService.RestoreVersionAsync(fileToken, versionToken, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{versionToken}")]
    public async Task<IActionResult> DeleteVersion(
        [FromRoute] string fileToken,
        [FromRoute] string versionToken,
        CancellationToken cancellationToken)
    {
        await _versionService.DeleteVersionAsync(fileToken, versionToken, cancellationToken);
        return NoContent();
    }
}
