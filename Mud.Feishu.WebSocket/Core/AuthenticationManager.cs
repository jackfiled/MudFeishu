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
                    var delay = exponentialDelay > maxDelay ? maxDelay : exponentialDelay;

                    _logger.LogInformation("等待 {Delay}ms 后进行第 {NextAttempt} 次认证尝试",
                        delay.TotalMilliseconds, attempt + 2);

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
                _logger.LogError("WebSocket认证失败: {Code} - {Message}", authResponse?.Code, authResponse?.Message);

                // 认证失败时重置会话
                _sessionManager?.ResetSession();


                var errorArgs = new WebSocketErrorEventArgs
                {
                    ErrorMessage = $"WebSocket认证失败: {authResponse?.Code} - {authResponse?.Message}",
                    IsAuthError = true
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
    /// 获取认证重试次数
    /// </summary>
    public int AuthRetryCount => _authRetryCount;
}