using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeishuFileServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : FeishuControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IFileService fileService, ILogger<FilesController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<FileUploadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<FileUploadResponse>>> UploadFile(
        IFormFile file,
        [FromForm] string? folderToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _fileService.UploadFileAsync(file, folderToken, userId, cancellationToken);
            return Success(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequestResult<FileUploadResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return ServerError<FileUploadResponse>("Error uploading file");
        }
    }

    [HttpGet("{fileToken}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
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
                return NotFoundError("File not found");
            }

            var content = await _fileService.DownloadFileAsync(fileToken, versionToken, cancellationToken);

            var mimeType = fileInfo.MimeType ?? "application/octet-stream";

            return File(content, mimeType, fileInfo.FileName);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundError("File not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileToken}", fileToken);
            return ServerErrorResult("Error downloading file");
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<FileListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FileListResponse>>> GetFiles(
        [FromQuery] string? folderToken,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _fileService.GetFilesAsync(folderToken, userId, search, page, pageSize, cancellationToken);
        return Success(result);
    }

    [HttpPut("{fileToken}/rename")]
    [ProducesResponseType(typeof(ApiResponse<FileInfoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<FileInfoResponse>>> RenameFile(
        string fileToken,
        [FromBody] RenameFileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _fileService.RenameFileAsync(fileToken, request.NewName, cancellationToken);
            if (result == null)
            {
                return NotFoundResult<FileInfoResponse>("File not found");
            }
            return Success(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequestResult<FileInfoResponse>(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFoundResult<FileInfoResponse>("File not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming file {FileToken}", fileToken);
            return ServerError<FileInfoResponse>("Error renaming file");
        }
    }

    [HttpGet("{fileToken}")]
    [ProducesResponseType(typeof(ApiResponse<FileInfoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FileInfoResponse>>> GetFile(
        string fileToken,
        CancellationToken cancellationToken = default)
    {
        var file = await _fileService.GetFileAsync(fileToken, cancellationToken);
        if (file == null)
        {
            return NotFoundResult<FileInfoResponse>("File not found");
        }
        return Success(file);
    }

    [HttpDelete("{fileToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
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
            return NotFoundError("File not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileToken}", fileToken);
            return ServerErrorResult("Error deleting file");
        }
    }
}
