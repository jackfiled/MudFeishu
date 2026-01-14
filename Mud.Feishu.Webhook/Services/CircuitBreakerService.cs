// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook;

/// <summary>
/// 断路器状态
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// 关闭状态：正常工作
    /// </summary>
    Closed,

    /// <summary>
    /// 开启状态：已断开，拒绝请求
    /// </summary>
    Open,

    /// <summary>
    /// 半开状态：尝试恢复
    /// </summary>
    HalfOpen
}

/// <summary>
/// 断路器配置选项
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// 断路器断开前的连续失败次数，默认 5
    /// </summary>
    public int ExceptionsAllowedBeforeBreaking { get; set; } = 5;

    /// <summary>
    /// 断路器保持开启状态的持续时间，默认 30 秒
    /// </summary>
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 断路器进入半开状态后的成功次数阈值，默认 3
    /// 达到此成功次数后，断路器重置为关闭状态
    /// </summary>
    public int SuccessThresholdToReset { get; set; } = 3;
}

/// <summary>
/// 断路器服务
/// 用于保护下游服务，防止级联故障
/// </summary>
public class CircuitBreakerService
{
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<CircuitBreakerService> _logger;
    private readonly object _lock = new();
    private int _failureCount;
    private int _successCountInHalfOpen;
    private DateTimeOffset? _circuitOpenedTime;
    private CircuitState _state = CircuitState.Closed;

    /// <summary>
    /// 当前断路器状态
    /// </summary>
    public CircuitState State => _state;

    /// <summary>
    /// 失败计数
    /// </summary>
    public int FailureCount => _failureCount;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CircuitBreakerService(
        CircuitBreakerOptions? options = null,
        ILogger<CircuitBreakerService>? logger = null)
    {
        _options = options ?? new CircuitBreakerOptions();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CircuitBreakerService>.Instance;

        _logger.LogInformation("断路器服务初始化完成，失败阈值: {FailureThreshold}, 断开时长: {BreakDuration}",
            _options.ExceptionsAllowedBeforeBreaking, _options.DurationOfBreak);
    }

    /// <summary>
    /// 执行操作，带断路器保护
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="action">要执行的操作</param>
    /// <returns>操作结果</returns>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        if (!IsRequestAllowed())
        {
            throw new CircuitBreakerOpenException("断路器已开启，拒绝请求");
        }

        try
        {
            var result = await action();
            RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            throw;
        }
    }

    /// <summary>
    /// 执行无返回值的操作，带断路器保护
    /// </summary>
    public async Task ExecuteAsync(Func<Task> action)
    {
        if (!IsRequestAllowed())
        {
            throw new CircuitBreakerOpenException("断路器已开启，拒绝请求");
        }

        try
        {
            await action();
            RecordSuccess();
        }
        catch (Exception ex)
        {
            RecordFailure(ex);
            throw;
        }
    }

    /// <summary>
    /// 检查是否允许请求通过
    /// </summary>
    private bool IsRequestAllowed()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitState.Closed:
                    return true;

                case CircuitState.Open:
                    // 检查是否可以进入半开状态
                    if (_circuitOpenedTime.HasValue && DateTimeOffset.UtcNow - _circuitOpenedTime >= _options.DurationOfBreak)
                    {
                        _state = CircuitState.HalfOpen;
                        _successCountInHalfOpen = 0;
                        _logger.LogInformation("断路器进入半开状态，尝试恢复服务");
                        return true;
                    }
                    var waitTime = _options.DurationOfBreak - (DateTimeOffset.UtcNow - _circuitOpenedTime!.Value);
                    _logger.LogWarning("断路器已开启，拒绝请求，将在 {WaitTime} 后尝试恢复", waitTime);
                    return false;

                case CircuitState.HalfOpen:
                    return true;

                default:
                    return true;
            }
        }
    }

    /// <summary>
    /// 记录成功
    /// </summary>
    private void RecordSuccess()
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitState.Closed:
                    _failureCount = 0;
                    break;

                case CircuitState.HalfOpen:
                    _successCountInHalfOpen++;
                    if (_successCountInHalfOpen >= _options.SuccessThresholdToReset)
                    {
                        _state = CircuitState.Closed;
                        _failureCount = 0;
                        _successCountInHalfOpen = 0;
                        _circuitOpenedTime = null;
                        _logger.LogInformation("断路器已重置为关闭状态");
                    }
                    break;

                case CircuitState.Open:
                    // 不应该发生，保持在开启状态
                    break;
            }
        }
    }

    /// <summary>
    /// 记录失败
    /// </summary>
    private void RecordFailure(Exception exception)
    {
        lock (_lock)
        {
            switch (_state)
            {
                case CircuitState.Closed:
                    _failureCount++;
                    _logger.LogWarning("断路器记录失败，当前失败计数: {FailureCount}/{Threshold}",
                        _failureCount, _options.ExceptionsAllowedBeforeBreaking);

                    if (_failureCount >= _options.ExceptionsAllowedBeforeBreaking)
                    {
                        _state = CircuitState.Open;
                        _circuitOpenedTime = DateTimeOffset.UtcNow;
                        _logger.LogError(exception, "断路器已开启，连续失败次数: {FailureCount}", _failureCount);
                    }
                    break;

                case CircuitState.HalfOpen:
                    _state = CircuitState.Open;
                    _circuitOpenedTime = DateTimeOffset.UtcNow;
                    _logger.LogError(exception, "断路器在半开状态中再次失败，重新开启");
                    break;

                case CircuitState.Open:
                    // 已经开启，保持状态
                    break;
            }
        }
    }

    /// <summary>
    /// 手动重置断路器
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitState.Closed;
            _failureCount = 0;
            _successCountInHalfOpen = 0;
            _circuitOpenedTime = null;
            _logger.LogInformation("断路器已手动重置为关闭状态");
        }
    }

    /// <summary>
    /// 手动开启断路器
    /// </summary>
    public void Trip()
    {
        lock (_lock)
        {
            _state = CircuitState.Open;
            _circuitOpenedTime = DateTimeOffset.UtcNow;
            _logger.LogWarning("断路器已手动开启");
        }
    }
}

/// <summary>
/// 断路器开启异常
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误消息</param>
    public CircuitBreakerOpenException(string message) : base(message)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
