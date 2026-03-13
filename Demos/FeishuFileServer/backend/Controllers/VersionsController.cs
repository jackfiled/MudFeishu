using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 版本控制器
/// 提供文件版本的查询、创建、下载、恢复和删除等API接口
/// </summary>
[ApiController]
[Route("api/files/{fileToken}/versions")]
[Authorize]
public class VersionsController : ControllerBase
{
    private readonly IVersionService _versionService;
    private readonly ILogger<VersionsController> _logger;

    /// <summary>
    /// 初始化版本控制器实例
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
    /// 获取文件版本列表
    /// <para>获取指定文件的所有历史版本</para>
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>版本列表响应</returns>
    [HttpGet]
    [ProducesResponseType(typeof(VersionListResponse), StatusCodes.Status200OK)]
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

    /// <summary>
    /// 创建新版本
    /// <para>为文件创建新的版本记录</para>
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="file">上传的文件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的版本信息</returns>
    [HttpPost]
    [ProducesResponseType(typeof(VersionCreateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    /// <summary>
    /// 下载指定版本
    /// <para>下载文件的历史版本</para>
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="versionToken">版本令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容</returns>
    [HttpGet("{versionToken}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
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
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Version not found" });
        }
    }

    /// <summary>
    /// 恢复到指定版本
    /// <para>将文件恢复到指定的历史版本</para>
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="versionToken">版本令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>恢复结果</returns>
    [HttpPut("{versionToken}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// <para>删除文件的历史版本</para>
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="versionToken">版本令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{versionToken}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVersion(
        [FromRoute] string fileToken,
        [FromRoute] string versionToken,
        CancellationToken cancellationToken)
    {
        await _versionService.DeleteVersionAsync(fileToken, versionToken, cancellationToken);
        return NoContent();
    }
}
