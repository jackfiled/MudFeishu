using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 文件版本管理控制器
/// 提供文件版本的查询、创建、下载、恢复、删除等功能
/// </summary>
[ApiController]
[Route("api/files/{fileToken}/versions")]
[Authorize]
public class VersionsController : FeishuControllerBase
{
    private readonly IVersionService _versionService;
    private readonly ILogger<VersionsController> _logger;

    /// <summary>
    /// 初始化版本控制器
    /// </summary>
    /// <param name="versionService">版本服务</param>
    /// <param name="logger">日志记录器</param>
    public VersionsController(
        IVersionService versionService,
        ILogger<VersionsController> logger)
    {
        _versionService = versionService;
        _logger = logger;
    }

    /// <summary>
    /// 获取文件的所有版本历史
    /// </summary>
    /// <param name="fileToken">文件Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>版本列表响应</returns>
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

    /// <summary>
    /// 创建新版本（上传文件作为新版本）
    /// </summary>
    /// <param name="fileToken">文件Token</param>
    /// <param name="file">新版本文件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>版本创建响应</returns>
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

    /// <summary>
    /// 下载指定版本的文件
    /// </summary>
    /// <param name="fileToken">文件Token</param>
    /// <param name="versionToken">版本Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容</returns>
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

    /// <summary>
    /// 恢复到指定版本
    /// </summary>
    /// <param name="fileToken">文件Token</param>
    /// <param name="versionToken">要恢复的版本Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>无内容</returns>
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

    /// <summary>
    /// 删除指定版本
    /// </summary>
    /// <param name="fileToken">文件Token</param>
    /// <param name="versionToken">要删除的版本Token</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>无内容</returns>
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
