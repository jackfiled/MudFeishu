// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuOAuthDemo.Models;
using FeishuOAuthDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Mud.Feishu;
using Mud.Feishu.TokenManager;

namespace FeishuOAuthDemo.Controllers;

/// <summary>
/// 飞书OAuth认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OAuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IUserTokenManager _userTokenManager;
    private readonly IStateStorageService _stateStorageService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserService _userService;
    private readonly IFeishuUserV3User _feishuUserApi;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IConfiguration configuration,
        IUserTokenManager userTokenManager,
        IStateStorageService stateStorageService,
        IJwtTokenService jwtTokenService,
        IUserService userService,
        IFeishuUserV3User feishuUserApi,
        ILogger<OAuthController> logger)
    {
        _configuration = configuration;
        _userTokenManager = userTokenManager;
        _stateStorageService = stateStorageService;
        _jwtTokenService = jwtTokenService;
        _userService = userService;
        _feishuUserApi = feishuUserApi;
        _logger = logger;
    }

    /// <summary>
    /// 获取飞书授权URL
    /// </summary>
    /// <returns>授权URL和state参数</returns>
    [HttpGet("feishu/url")]
    public IActionResult GetFeishuAuthUrl()
    {
        try
        {
            var appId = _configuration["Feishu:AppId"];
            var redirectUri = _configuration["OAuth:RedirectUri"];

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(redirectUri))
            {
                return BadRequest(new AuthUrlResponse
                {
                    Success = false,
                    Message = "飞书应用配置不完整"
                });
            }

            // 生成state参数（防CSRF攻击）
            var state = _stateStorageService.GenerateState();

            // 构建飞书授权URL
            var authUrl = $"https://accounts.feishu.cn/open-apis/authen/v1/authorize?" +
                          $"client_id={appId}&" +
                          $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                          $"response_type=code&" +
                          $"scope=contact:user.base:readonly&" +
                          $"state={state}";

            _logger.LogInformation("生成飞书授权URL成功，State: {State}", state);

            return Ok(new AuthUrlResponse
            {
                Success = true,
                Message = "生成授权URL成功",
                Url = authUrl,
                State = state
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成飞书授权URL失败");
            return StatusCode(500, new AuthUrlResponse
            {
                Success = false,
                Message = $"生成授权URL失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// 处理飞书OAuth回调
    /// </summary>
    /// <param name="request">回调请求（包含code和state）</param>
    /// <returns>登录结果和JWT令牌</returns>
    [HttpPost("feishu/callback")]
    public async Task<IActionResult> HandleFeishuCallback([FromBody] AuthCallbackRequest request)
    {
        try
        {
            _logger.LogInformation("收到飞书OAuth回调，Code: {Code}, State: {State}", request.Code[..8] + "...", request.State);

            // 1. 验证请求参数
            if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.State))
            {
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "缺少必要参数"
                });
            }

            // 2. 验证state参数（防CSRF攻击）
            if (!_stateStorageService.ValidateState(request.State))
            {
                _logger.LogWarning("State验证失败: {State}", request.State);
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "State验证失败，可能存在CSRF攻击"
                });
            }

            // 移除已使用的state
            _stateStorageService.RemoveState(request.State);

            var redirectUri = _configuration["OAuth:RedirectUri"];

            // 3. 使用授权码获取用户访问令牌
            _logger.LogInformation("开始使用授权码获取用户访问令牌");
            var tokenResult = await _userTokenManager.GetUserTokenWithCodeAsync(request.Code, redirectUri ?? string.Empty);

            if (tokenResult == null || tokenResult.Code != 0)
            {
                _logger.LogError("获取用户访问令牌失败: {Message}", tokenResult?.Msg ?? "未知错误");
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = $"获取用户访问令牌失败: {tokenResult?.Msg ?? "未知错误"}"
                });
            }

            _logger.LogInformation("成功获取用户访问令牌");

            // 4. 获取用户信息
            _logger.LogInformation("开始获取用户信息");
            var userInfoResult = await _feishuUserApi.GetUserInfoAsync();

            if (userInfoResult?.Data == null)
            {
                _logger.LogError("获取用户信息失败: {Message}", userInfoResult?.Msg ?? "未知错误");
                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = $"获取用户信息失败: {userInfoResult?.Msg ?? "未知错误"}"
                });
            }

            var feishuUser = userInfoResult.Data;
            _logger.LogInformation("成功获取用户信息: {Name} ({OpenId})", feishuUser.Name ?? "未知", feishuUser.OpenId ?? "未知");

            // 5. 在本地系统中获取或创建用户
            var userId = await _userService.GetOrCreateUserAsync(
                feishuUser.OpenId ?? string.Empty,
                feishuUser.UnionId ?? string.Empty,
                feishuUser.Name ?? "未知用户",
                null,
                feishuUser.Email
            );

            _logger.LogInformation("用户处理完成，本地用户ID: {UserId}", userId);

            // 6. 生成JWT令牌
            var jwtToken = _jwtTokenService.GenerateToken(
                feishuUser.OpenId ?? string.Empty,
                feishuUser.UnionId ?? string.Empty,
                feishuUser.Name ?? "未知用户"
            );

            _logger.LogInformation("JWT令牌生成成功");

            // 7. 返回登录结果
            return Ok(new LoginResponse
            {
                Success = true,
                Message = "登录成功",
                Token = jwtToken,
                User = new UserInfoResponse
                {
                    OpenId = feishuUser.OpenId ?? string.Empty,
                    UnionId = feishuUser.UnionId ?? string.Empty,
                    Name = feishuUser.Name ?? "未知用户",
                    Email = feishuUser.Email
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理飞书OAuth回调失败");
            return StatusCode(500, new LoginResponse
            {
                Success = false,
                Message = $"登录失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// 验证JWT令牌
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>验证结果</returns>
    [HttpPost("validate-token")]
    public IActionResult ValidateToken([FromBody] string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { success = false, message = "令牌不能为空" });
            }

            var isValid = _jwtTokenService.ValidateToken(token, out var principal);

            if (!isValid || principal == null)
            {
                return Unauthorized(new { success = false, message = "令牌无效或已过期" });
            }

            var userInfo = _jwtTokenService.GetUserFromToken(token);
            return Ok(new
            {
                success = true,
                message = "令牌有效",
                user = new
                {
                    openId = userInfo?.openId,
                    unionId = userInfo?.unionId,
                    name = userInfo?.name
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证令牌失败");
            return StatusCode(500, new { success = false, message = "验证令牌失败" });
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    /// <returns>用户信息</returns>
    [HttpGet("user/me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            // 从Authorization头获取令牌
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized(new { success = false, message = "缺少授权令牌" });
            }

            var token = authHeader["Bearer ".Length..].Trim();
            var userInfo = _jwtTokenService.GetUserFromToken(token);

            if (userInfo == null)
            {
                return Unauthorized(new { success = false, message = "令牌无效" });
            }

            // 获取完整用户信息
            var user = await _userService.GetUserByIdAsync(userInfo.Value.openId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "用户不存在" });
            }

            return Ok(new
            {
                success = true,
                user = new
                {
                    userId = user.UserId,
                    openId = user.OpenId,
                    unionId = user.UnionId,
                    name = user.Name,
                    avatar = user.Avatar,
                    email = user.Email,
                    createdAt = user.CreatedAt,
                    lastLoginAt = user.LastLoginAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取当前用户信息失败");
            return StatusCode(500, new { success = false, message = "获取用户信息失败" });
        }
    }

    /// <summary>
    /// 登出
    /// </summary>
    /// <returns>登出结果</returns>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // JWT是无状态的，登出主要是前端删除令牌
        // 后端可以维护一个黑名单（可选）
        return Ok(new { success = true, message = "登出成功" });
    }
}
