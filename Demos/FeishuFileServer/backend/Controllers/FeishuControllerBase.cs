// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuFileServer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FeishuFileServer.Controllers;

[ApiController]
public abstract class FeishuControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
{
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    protected int GetRequiredUserId()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new UnauthorizedAccessException("未登录");
        }
        return userId.Value;
    }

    protected ActionResult<ApiResponse<T>> Success<T>(T? data, string? message = null)
    {
        return Ok(ApiResponse<T>.Ok(data, message));
    }

    protected ActionResult<ApiResponse> Success(string? message = null)
    {
        return Ok(ApiResponse.Ok(message));
    }

    protected ActionResult<ApiResponse> BadRequestResult(string message)
    {
        return BadRequest(ApiResponse.Fail(message, 400));
    }

    protected ActionResult<ApiResponse<T>> BadRequestResult<T>(string message)
    {
        return BadRequest(ApiResponse<T>.Fail(message, 400));
    }

    protected ActionResult<ApiResponse> UnauthorizedResult(string message = "未登录")
    {
        return Unauthorized(ApiResponse.Fail(message, 401));
    }

    protected ActionResult<ApiResponse<T>> UnauthorizedResult<T>(string message = "未登录")
    {
        return Unauthorized(ApiResponse<T>.Fail(message, 401));
    }

    protected ActionResult<ApiResponse> NotFoundResult(string message = "资源不存在")
    {
        return NotFound(ApiResponse.Fail(message, 404));
    }

    protected ActionResult<ApiResponse<T>> NotFoundResult<T>(string message = "资源不存在")
    {
        return NotFound(ApiResponse<T>.Fail(message, 404));
    }

    protected IActionResult ServerErrorResult(string message)
    {
        return StatusCode(500, ApiResponse.Fail(message, 500));
    }

    protected ActionResult<ApiResponse> ServerError(string message)
    {
        return StatusCode(500, ApiResponse.Fail(message, 500));
    }

    protected ActionResult<ApiResponse<T>> ServerError<T>(string message)
    {
        return StatusCode(500, ApiResponse<T>.Fail(message, 500));
    }

    protected ActionResult<ApiResponse<T>> CreatedResult<T>(string actionName, object routeValues, T? data)
    {
        return CreatedAtAction(actionName, routeValues, ApiResponse<T>.Ok(data));
    }

    protected IActionResult NotFoundError(string message = "资源不存在")
    {
        return NotFound(ApiResponse.Fail(message, 404));
    }

    protected IActionResult UnauthorizedError(string message = "未登录")
    {
        return Unauthorized(ApiResponse.Fail(message, 401));
    }

    protected IActionResult BadRequestError(string message)
    {
        return BadRequest(ApiResponse.Fail(message, 400));
    }
}
