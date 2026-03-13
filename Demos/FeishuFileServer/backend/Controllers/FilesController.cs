using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileService fileService, ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileUploadResponse>> UploadFile(
        IFormFile file,
        [FromForm] string? folderToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _fileService.UploadFileAsync(file, folderToken, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, new { message = "Error uploading file", error = ex.Message });
        }
    }

    [HttpGet("{fileToken}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(
        string fileToken,
        [FromQuery] string? versionToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var fileInfo = await _fileService.GetFileAsync(fileToken, cancellationToken);
            if (fileInfo == null)
            {
                return NotFound(new { message = "File not found" });
            }

            var content = await _fileService.DownloadFileAsync(fileToken, versionToken, cancellationToken);
            
            var mimeType = fileInfo.MimeType ?? "application/octet-stream";
            var fileName = Uri.EscapeDataString(fileInfo.FileName);

            return File(content, mimeType, fileInfo.FileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "File not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileToken}", fileToken);
            return StatusCode(500, new { message = "Error downloading file", error = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(FileListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FileListResponse>> GetFiles(
        [FromQuery] string? folderToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _fileService.GetFilesAsync(folderToken, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{fileToken}")]
    [ProducesResponseType(typeof(FileInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileInfoResponse>> GetFile(
        string fileToken,
        CancellationToken cancellationToken = default)
    {
        var file = await _fileService.GetFileAsync(fileToken, cancellationToken);
        if (file == null)
        {
            return NotFound(new { message = "File not found" });
        }
        return Ok(file);
    }

    [HttpDelete("{fileToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFile(
        string fileToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _fileService.DeleteFileAsync(fileToken, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "File not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileToken}", fileToken);
            return StatusCode(500, new { message = "Error deleting file", error = ex.Message });
        }
    }
}
