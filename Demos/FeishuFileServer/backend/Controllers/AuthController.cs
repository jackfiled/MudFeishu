// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FeishuFileServer.Controllers;

/// <summary>
/// 认证控制器
/// 提供用户登录、注册、个人信息管理和密码修改等API接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// 初始化认证控制器实例
    /// </summary>
    /// <param name="authService">认证服务</param>
    /// <param name="logger">日志记录器</param>
    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 用户登录
    /// <para>验证用户名和密码，成功后返回JWT令牌</para>
    /// </summary>
    /// <param name="request">登录请求，包含用户名和密码</param>
    /// <returns>登录成功返回令牌和用户信息，失败返回401</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            if (response == null)
            {
                return Unauthorized(new { message = "用户名或密码错误" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录失败");
            return StatusCode(500, new { message = "登录失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 用户注册
    /// <para>创建新用户账户并返回JWT令牌</para>
    /// </summary>
    /// <param name="request">注册请求，包含用户名、密码等信息</param>
    /// <returns>注册成功返回令牌和用户信息</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            if (response == null)
            {
                return BadRequest(new { message = "注册失败" });
            }

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册失败");
            return StatusCode(500, new { message = "注册失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// <para>需要登录认证</para>
    /// </summary>
    /// <returns>当前登录用户的信息</returns>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserInfo>> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        var userInfo = await _authService.GetUserInfoAsync(userId.Value);
        if (userInfo == null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        return Ok(userInfo);
    }

    /// <summary>
    /// 更新当前用户资料
    /// <para>更新用户的邮箱和显示名称，需要登录认证</para>
    /// </summary>
    /// <param name="request">更新资料请求</param>
    /// <returns>更新后的用户信息</returns>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfo>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        try
        {
            var userInfo = await _authService.UpdateProfileAsync(userId.Value, request);
            if (userInfo == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            return Ok(userInfo);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户信息失败");
            return StatusCode(500, new { message = "更新失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 修改当前用户密码
    /// <para>需要验证当前密码，需要登录认证</para>
    /// </summary>
    /// <param name="request">修改密码请求，包含当前密码和新密码</param>
    /// <returns>修改结果</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        var result = await _authService.ChangePasswordAsync(userId.Value, request);
        if (!result)
        {
            return BadRequest(new { message = "当前密码错误" });
        }

        return Ok(new { message = "密码修改成功" });
    }

    /// <summary>
    /// 刷新访问令牌
    /// <para>使用刷新令牌获取新的访问令牌</para>
    /// </summary>
    /// <param name="request">刷新令牌请求</param>
    /// <returns>新的登录响应</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken);
            if (response == null)
            {
                return Unauthorized(new { message = "无效的刷新令牌" });
            }

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新令牌失败");
            return StatusCode(500, new { message = "刷新令牌失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 撤销刷新令牌
    /// <para>使刷新令牌失效，需要登录认证</para>
    /// </summary>
    /// <param name="request">刷新令牌请求</param>
    /// <returns>撤销结果</returns>
    [HttpPost("revoke-token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "未登录" });
        }

        await _authService.RevokeRefreshTokenAsync(request.RefreshToken, userId.Value);
        return Ok(new { message = "令牌已撤销" });
    }

    /// <summary>
    /// 从当前请求的JWT令牌中获取用户ID
    /// </summary>
    /// <returns>用户ID，未登录时返回null</returns>
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
