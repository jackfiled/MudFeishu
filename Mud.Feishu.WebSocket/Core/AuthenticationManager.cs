// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.WebSocket.DataModels;
using Mud.Feishu.WebSocket.SocketEventArgs;
using System.Text.Json;

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 认证管理器 - 处理WebSocket认证相关逻辑
/// </summary>
public class AuthenticationManager
{
    private readonly ILogger<AuthenticationManager> _logger;
    private readonly Func<string, Task> _sendMessageCallback;
    private readonly SessionManager? _sessionManager;
    private bool _isAuthenticated = false;
    private readonly FeishuWebSocketOptions _options;
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private int _authRetryCount = 0;
    private int _totalAuthFailures = 0;
    private DateTime _lastAuthFailureTime = DateTime.MinValue;

    /// <summary>
    /// 认证成功事件
    /// </summary>
    public event EventHandler<EventArgs>? Authenticated;

    /// <summary>
    /// 认证失败事件
    /// </summary>
    public event EventHandler<WebSocketErrorEventArgs>? AuthenticationFailed;

    /// <summary>
    /// 获取当前认证状态
    /// </summary>
    /// <returns>如果已认证返回true，否则返回false</returns>
    public bool IsAuthenticated => _isAuthenticated;

    /// <summary>
    /// 初始化认证管理器实例
    /// </summary>
    /// <param name="logger">日志记录器实例</param>
    /// <param name="options">WebSocket配置选项</param>
    /// <param name="sendMessageCallback">发送消息回调函数</param>
    /// <param name="sessionManager">会话管理器（可选）</param>
    public AuthenticationManager(
        ILogger<AuthenticationManager> logger,
        FeishuWebSocketOptions options,
        Func<string, Task> sendMessageCallback,
        SessionManager? sessionManager = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sendMessageCallback = sendMessageCallback ?? throw new ArgumentNullException(nameof(sendMessageCallback));
        _sessionManager = sessionManager;
        _options = options;
    }

    /// <summary>
    /// 发送认证消息（带重试机制）
    /// </summary>
    public async Task AuthenticateAsync(string appAccessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(appAccessToken))
            throw new ArgumentException("应用访问令牌不能为空", nameof(appAccessToken));

        await _authLock.WaitAsync(cancellationToken);
        try
        {
            // 如果已认证，直接返回
            if (_isAuthenticated)
            {
                _logger.LogDebug("WebSocket已认证，跳过重复认证");
                return;
            }

            // 使用指数退避策略重试认证
            var maxRetries = _options.MaxReconnectAttempts;
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _authRetryCount = attempt;
                    await AuthenticateInternalAsync(appAccessToken, cancellationToken);
                    // 认证成功，退出重试循环
                    break;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "WebSocket认证失败（第 {Attempt} 次尝试），准备重试...", attempt + 1);

                    // 计算退避延迟时间：baseDelay * (2^attempt)，最大不超过 MaxReconnectDelayMs
                    var baseDelay = TimeSpan.FromMilliseconds(_options.ReconnectDelayMs);
                    var exponentialDelay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
                    var maxDelay = TimeSpan.FromMilliseconds(_options.MaxReconnectDelayMs);

                    // 添加随机抖动，避免多个客户端同时重试造成雪崩
                    var random = new Random();
                    var jitter = random.Next(0, 1000); // 0-1000ms 的随机抖动
                    var delay = exponentialDelay > maxDelay ? maxDelay : exponentialDelay;
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds + jitter);

                    _logger.LogInformation("等待 {Delay}ms 后进行第 {NextAttempt} 次认证尝试（含 {Jitter}ms 抖动）",
                        delay.TotalMilliseconds, attempt + 2, jitter);

                    await Task.Delay(delay, cancellationToken);
                }
            }
        }
        finally
        {
            _authLock.Release();
        }
    }

    /// <summary>
    /// 内部认证实现
    /// </summary>
    private async Task AuthenticateInternalAsync(string appAccessToken, CancellationToken cancellationToken)
    {
        try
        {
            if (_authRetryCount > 0)
            {
                _logger.LogInformation("正在进行WebSocket认证（重试第 {RetryCount} 次）...", _authRetryCount);
            }
            else
            {
                _logger.LogInformation("正在进行WebSocket认证...");
            }

            _isAuthenticated = false; // 重置认证状态

            // 创建认证消息
            var authMessage = new AuthMessage
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Data = new AuthData
                {
                    AppAccessToken = appAccessToken,
                    // 尝试使用缓存的 session_id 进行会话恢复
                    SessionId = _sessionManager?.GetSessionIdForReconnect()
                }
            };

            var authJson = JsonSerializer.Serialize(authMessage, JsonOptions.Default);
            await _sendMessageCallback(authJson);

            if (_options.EnableLogging)
            {
                _logger.LogInformation("已发送认证消息，等待响应...");
            }
        }
        catch (Exception ex)
        {
            _isAuthenticated = false;
            _logger.LogError(ex, "WebSocket认证失败（第 {Attempt} 次尝试）", _authRetryCount + 1);

            var errorArgs = new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = $"WebSocket认证失败: {ex.Message}",
                ErrorType = ex.GetType().Name,
                IsAuthError = true
            };

            AuthenticationFailed?.Invoke(this, errorArgs);

            // 如果是最后一次尝试，抛出异常；否则由外层重试
            if (_authRetryCount >= _options.MaxReconnectAttempts)
            {
                throw new InvalidOperationException($"WebSocket认证失败，已达到最大重试次数 {_options.MaxReconnectAttempts}", ex);
            }

            throw;
        }
    }

    /// <summary>
    /// 处理认证响应
    /// </summary>
    public void HandleAuthResponse(string responseMessage)
    {
        try
        {
            var authResponse = JsonSerializer.Deserialize<AuthResponseMessage>(responseMessage);

            if (authResponse?.Code == 0)
            {
                _isAuthenticated = true;
                _logger.LogInformation("WebSocket认证成功: {Message}", authResponse.Message);

                // 如果响应中包含 session_id，保存到会话管理器
                if (!string.IsNullOrEmpty(authResponse.SessionId) && _sessionManager != null)
                {
                    _sessionManager.SetSessionId(authResponse.SessionId);
                }

                Authenticated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                _isAuthenticated = false;
                _totalAuthFailures++;
                _lastAuthFailureTime = DateTime.UtcNow;

                _logger.LogError("WebSocket认证失败: {Code} - {Message}, 总失败次数: {TotalFailures}",
                    authResponse?.Code, authResponse?.Message, _totalAuthFailures);

                // 认证失败时重置会话
                _sessionManager?.ResetSession();

                // 根据不同的错误码记录详细信息
                var errorCode = authResponse?.Code;
                var errorMessage = authResponse?.Message;
                LogDetailedAuthError(errorCode, errorMessage);

                var errorArgs = new WebSocketErrorEventArgs
                {
                    ErrorMessage = $"WebSocket认证失败: {errorCode} - {errorMessage}",
                    IsAuthError = true,
                    ErrorType = $"AuthError_{errorCode}",
                    Exception = new InvalidOperationException($"认证失败: {errorCode} - {errorMessage}")
                };

                AuthenticationFailed?.Invoke(this, errorArgs);
            }
        }
        catch (JsonException ex)
        {
            _isAuthenticated = false;
            _logger.LogError(ex, "解析认证响应失败: {Message}", responseMessage);

            var errorArgs = new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = $"解析认证响应失败: {ex.Message}",
                ErrorType = ex.GetType().Name,
                IsAuthError = true
            };

            AuthenticationFailed?.Invoke(this, errorArgs);
        }
        catch (Exception ex)
        {
            _isAuthenticated = false;
           _logger.LogError(ex, "处理认证响应时发生错误");

            var errorArgs = new WebSocketErrorEventArgs
            {
                Exception = ex,
                ErrorMessage = $"处理认证响应时发生错误: {ex.Message}",
                ErrorType = ex.GetType().Name,
                IsAuthError = true
            };

            AuthenticationFailed?.Invoke(this, errorArgs);
        }
    }

    /// <summary>
    /// 重置认证状态
    /// </summary>
    public void ResetAuthentication()
    {
        _isAuthenticated = false;
        _authRetryCount = 0;
        _logger.LogDebug("已重置认证状态");
    }

    /// <summary>
    /// 获取认证重试次数（当前认证流程）
    /// </summary>
    public int AuthRetryCount => _authRetryCount;

    /// <summary>
    /// 获取总认证失败次数
    /// </summary>
    public int TotalAuthFailures => _totalAuthFailures;

    /// <summary>
    /// 获取最近一次认证失败时间
    /// </summary>
    public DateTime LastAuthFailureTime => _lastAuthFailureTime;

    /// <summary>
    /// 记录详细的认证错误信息
    /// </summary>
    /// <param name="errorCode">错误码</param>
    /// <param name="errorMessage">错误消息</param>
    private void LogDetailedAuthError(int? errorCode, string? errorMessage)
    {
        if (!errorCode.HasValue)
        {
            _logger.LogWarning("认证响应缺少错误码: {Message}", errorMessage ?? "未知错误");
            return;
        }

        switch (errorCode.Value)
        {
            case 10009: // Token 过期
                _logger.LogWarning("应用访问令牌已过期，请更新令牌");
                break;
            case 10010: // Token 无效
                _logger.LogError("应用访问令牌无效，请检查 App ID 和 App Secret 配置");
                break;
            case 10011: // 权限不足
                _logger.LogError("应用权限不足，请检查应用权限配置");
                break;
            case 10012: // 参数错误
                _logger.LogError("认证参数错误: {Message}", errorMessage);
                break;
            case 10013: // 系统繁忙
                _logger.LogWarning("飞书系统繁忙，建议稍后重试");
                break;
            case 10014: // 版本不支持
                _logger.LogError("WebSocket 版本不支持，请更新 SDK");
                break;
            case 10015: // Session ID 无效
                _logger.LogWarning("Session ID 无效，将重新建立会话");
                break;
            default:
                _logger.LogWarning("未知的认证错误码: {Code}, 消息: {Message}", errorCode.Value, errorMessage);
                break;
        }

        // 如果连续失败次数过多，记录警告
        if (_totalAuthFailures > 5)
        {
            _logger.LogWarning("认证已连续失败 {Count} 次，请检查网络连接和配置", _totalAuthFailures);
        }

        // 记录最近失败时间间隔（如果有）
        if (_lastAuthFailureTime != DateTime.MinValue)
        {
            var timeSinceLastFailure = DateTime.UtcNow - _lastAuthFailureTime;
            _logger.LogInformation("距上次认证失败时间: {Minutes}分钟", timeSinceLastFailure.TotalMinutes);
        }
    }
}