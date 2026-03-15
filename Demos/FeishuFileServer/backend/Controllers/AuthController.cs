using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services;

namespace FeishuFileServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : FeishuControllerBase
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
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            if (response == null)
            {
                return UnauthorizedResult<LoginResponse>("用户名或密码错误");
            }

            return Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录失败");
            return ServerError<LoginResponse>("登录失败，请稍后重试");
        }
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            if (response == null)
            {
                return BadRequestResult<LoginResponse>("注册失败");
            }

            return Success(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult<LoginResponse>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册失败");
            return ServerError<LoginResponse>("注册失败，请稍后重试");
        }
    }

    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserInfo>>> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<UserInfo>("未登录");
        }

        var userInfo = await _authService.GetUserInfoAsync(userId.Value);
        if (userInfo == null)
        {
            return NotFoundResult<UserInfo>("用户不存在");
        }

        return Success(userInfo);
    }

    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserInfo>>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult<UserInfo>("未登录");
        }

        try
        {
            var userInfo = await _authService.UpdateProfileAsync(userId.Value, request);
            if (userInfo == null)
            {
                return NotFoundResult<UserInfo>("用户不存在");
            }

            return Success(userInfo);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult<UserInfo>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户信息失败");
            return ServerError<UserInfo>("更新失败，请稍后重试");
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult("未登录");
        }

        var result = await _authService.ChangePasswordAsync(userId.Value, request);
        if (!result)
        {
            return BadRequestResult("当前密码错误");
        }

        return Success("密码修改成功");
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken);
            if (response == null)
            {
                return UnauthorizedResult<LoginResponse>("刷新令牌无效或已过期");
            }

            return Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新令牌失败");
            return UnauthorizedResult<LoginResponse>("刷新令牌无效或已过期");
        }
    }

    [HttpPost("revoke-token")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse>> RevokeToken([FromBody] RefreshTokenRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedResult("未登录");
        }

        try
        {
            await _authService.RevokeRefreshTokenAsync(request.RefreshToken, userId.Value);
            return Success("令牌已撤销");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤销令牌失败");
            return ServerError("撤销令牌失败");
        }
    }
}
