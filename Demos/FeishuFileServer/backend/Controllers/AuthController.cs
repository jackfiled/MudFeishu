using System.Security.Claims;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeishuFileServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
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

    [HttpPost("register")]
    [AllowAnonymous]
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

    [HttpGet("profile")]
    [Authorize]
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

    [HttpPut("profile")]
    [Authorize]
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

    [HttpPost("change-password")]
    [Authorize]
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
