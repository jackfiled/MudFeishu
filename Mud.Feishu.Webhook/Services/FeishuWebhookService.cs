// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Models;

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书 Webhook 服务实现
/// </summary>
public class FeishuWebhookService : IFeishuWebhookService
{
    private readonly IOptionsMonitor<FeishuWebhookOptions> _optionsMonitor;
    private readonly IFeishuEventValidator _validator;
    private readonly IFeishuEventDecryptor _decryptor;
    private readonly IFeishuEventHandlerFactory _handlerFactory;
    private readonly ILogger<FeishuWebhookService> _logger;
    private readonly IFeishuEventInterceptor[] _interceptors;
    private readonly FeishuWebhookConcurrencyService _concurrencyService;
    private readonly IFeishuEventDeduplicator _deduplicator;
    private readonly IFeishuEventDistributedDeduplicator? _distributedDeduplicator;
    private readonly ISecurityAuditService? _securityAuditService;
    private readonly IThreatDetectionService? _threatDetectionService;

    /// <summary>
    /// 提供的加密密钥（支持多密钥场景）
    /// </summary>
    private string? _providedEncryptKey;

    /// <summary>
    /// 获取当前配置选项（支持热更新）
    /// </summary>
    private FeishuWebhookOptions Options => _optionsMonitor.CurrentValue;

    /// <inheritdoc />
    public FeishuWebhookService(
        IOptionsMonitor<FeishuWebhookOptions> optionsMonitor,
        IFeishuEventValidator validator,
        IFeishuEventDecryptor decryptor,
        IFeishuEventHandlerFactory handlerFactory,
        ILogger<FeishuWebhookService> logger,
        IFeishuEventInterceptor[] interceptors,
        FeishuWebhookConcurrencyService concurrencyService,
        IFeishuEventDeduplicator deduplicator,
        ISecurityAuditService? securityAuditService,
        IFeishuEventDistributedDeduplicator? distributedDeduplicator = null,
        IThreatDetectionService? threatDetectionService = null)
    {
        _optionsMonitor = optionsMonitor;
        _validator = validator;
        _decryptor = decryptor;
        _handlerFactory = handlerFactory;
        _logger = logger;
        _interceptors = interceptors;
        _concurrencyService = concurrencyService;
        _deduplicator = deduplicator;
        _distributedDeduplicator = distributedDeduplicator;
        _securityAuditService = securityAuditService;
        _threatDetectionService = threatDetectionService;

        // 监听配置变更
        _optionsMonitor.OnChange((newOptions, name) =>
        {
            _logger.LogInformation("飞书 Webhook 配置已更新，来源: {ChangeSource}", name);
        });
    }

    /// <inheritdoc />
    public async Task<EventVerificationResponse?> VerifyEventSubscriptionAsync(EventVerificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("开始验证飞书事件订阅请求");

            // 注意：这里需要传入一个有效的 token，通常应该从应用配置中获取
            // 在多应用场景下，应该先设置当前应用键，然后验证器会自动使用对应的配置
            if (!_validator.ValidateSubscriptionRequest(request, string.Empty))
            {
                _logger.LogWarning("事件订阅验证失败");
                return null;
            }

            var response = new EventVerificationResponse
            {
                Challenge = request.Challenge
            };

            _logger.LogInformation("事件订阅验证成功，返回挑战码: {Challenge}", request.Challenge);
            return await Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证事件订阅请求时发生错误");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorReason)> HandleEventAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        return await HandleEventWithInterceptorsAsync(eventData, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? ErrorReason)> HandleEventAsync(FeishuWebhookRequest request, string? encryptKey = null, CancellationToken cancellationToken = default)
    {
        // 保存提供的密钥
        _providedEncryptKey = encryptKey;

        try
        {
            return await HandleEventWithInterceptorsAsync(request, null, cancellationToken);
        }
        finally
        {
            // 清理密钥
            _providedEncryptKey = null;
        }
    }

    /// <summary>
    /// 使用拦截器处理事件（已解密的 EventData）
    /// </summary>
    private async Task<(bool Success, string? ErrorReason)> HandleEventWithInterceptorsAsync(EventData eventData, string? appKey, CancellationToken cancellationToken)
    {
        Exception? processingException = null;

        try
        {
            // 前置拦截器
            foreach (var interceptor in _interceptors)
            {
                var shouldContinue = await interceptor.BeforeHandleAsync(eventData.EventType, eventData, cancellationToken);
                if (!shouldContinue)
                {
                    _logger.LogWarning("事件被拦截器中断: {EventType}, EventId: {EventId}, Interceptor: {InterceptorType}, AppKey: {AppKey}",
                        eventData.EventType, eventData.EventId, interceptor.GetType().Name, appKey ?? "null");
                    return (false, "Event intercepted");
                }
            }

            // 去重检查
            var deduplicationResult = await CheckDeduplicationAsync(eventData.EventId, appKey, cancellationToken);
            if (deduplicationResult.shouldSkip)
            {
                _logger.LogWarning("检测到重复事件 {EventId}（AppKey: {AppKey}），跳过处理（幂等性）", eventData.EventId, appKey ?? "null");
                return (true, null); // 幂等性：返回成功避免飞书重试
            }

            // 使用全局并发控制服务
            using var concurrencyLock = await _concurrencyService.AcquireAsync(cancellationToken);

            // 添加超时控制
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(Options.EventHandlingTimeoutMs);

            try
            {
                // 分发事件到处理器
                await _handlerFactory.HandleEventParallelAsync(eventData.EventType, eventData, timeoutCts.Token);

                // 处理成功，标记为已完成
                MarkDeduplicationCompletedAsync(eventData.EventId);

                _logger.LogInformation("事件处理完成: {EventType}, 事件ID: {EventId}, AppKey: {AppKey}",
                    eventData.EventType, eventData.EventId, appKey ?? "null");

                return (true, null);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                RollbackDeduplicationAsync(eventData.EventId);

                _logger.LogWarning("事件处理超时: {EventType}, 事件ID: {EventId}, 超时时间: {TimeoutMs}ms, AppKey: {AppKey}",
                    eventData.EventType, eventData.EventId, Options.EventHandlingTimeoutMs, appKey ?? "null");
                throw;
            }
        }
        catch (OperationCanceledException)
        {
            RollbackDeduplicationAsync(eventData.EventId);
            _logger.LogWarning("事件处理被取消，EventId: {EventId}, AppKey: {AppKey}", eventData.EventId, appKey ?? "null");
            throw;
        }
        catch (Exception ex)
        {
            processingException = ex;
            RollbackDeduplicationAsync(eventData.EventId);
            _logger.LogError(ex, "处理飞书事件时发生错误，EventId: {EventId}, AppKey: {AppKey}", eventData.EventId, appKey ?? "null");

            if (Options.EnableExceptionHandling)
            {
                return (false, "Internal server error");
            }
            throw;
        }
        finally
        {
            // 后置拦截器（无论成功或失败都执行）
            foreach (var interceptor in _interceptors)
            {
                await interceptor.AfterHandleAsync(eventData.EventType, eventData, processingException, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 使用拦截器处理事件（加密的 FeishuWebhookRequest）
    /// </summary>
    private async Task<(bool Success, string? ErrorReason)> HandleEventWithInterceptorsAsync(FeishuWebhookRequest request, string? appKey, CancellationToken cancellationToken)
    {
        // 验证请求签名
        if (Options.EnableBodySignatureValidation && !await ValidateRequestSignature(request))
        {
            _logger.LogWarning("请求体签名验证失败，AppKey: {AppKey}", appKey ?? "null");

            // 记录安全审计日志
            _ = _securityAuditService?.LogSecurityFailureAsync(
                SecurityEventType.SignatureValidation,
                "unknown", // 在服务层无法获取客户端IP
                "FeishuWebhookService",
                "请求体签名验证失败",
                "",
                appKey);

            return (false, "Signature validation failed");
        }

        // 解密事件数据
        if (string.IsNullOrEmpty(request.Encrypt))
        {
            _logger.LogError("请求中缺少加密数据，AppKey: {AppKey}", appKey ?? "null");
            return (false, "Missing encrypted data");
        }

        var eventData = await DecryptEventAsync(request.Encrypt!, cancellationToken);
        if (eventData == null)
        {
            _logger.LogError("事件数据解密失败，AppKey: {AppKey}", appKey ?? "null");
            return (false, "Decryption failed");
        }

        return await HandleEventWithInterceptorsAsync(eventData, appKey, cancellationToken);
    }

    /// <summary />
    public async Task<bool> ValidateRequestSignature(FeishuWebhookRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Encrypt) ||
                string.IsNullOrEmpty(request.Signature) ||
                string.IsNullOrEmpty(request.Nonce))
            {
                _logger.LogWarning("请求缺少必要的签名字段");
                return false;
            }

            // 在多应用场景下，应该使用提供的加密密钥
            if (string.IsNullOrEmpty(_providedEncryptKey))
            {
                _logger.LogError("缺少加密密钥，无法验证签名");
                return false;
            }

            return await _validator.ValidateSignatureAsync(
                request.Timestamp,
                request.Nonce,
                request.Encrypt!,
                request.Signature,
                _providedEncryptKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证请求签名时发生错误");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<EventData?> DecryptEventAsync(string encryptedData, CancellationToken cancellationToken = default)
    {
        try
        {
            // 在多应用场景下，应该使用提供的加密密钥
            if (string.IsNullOrEmpty(_providedEncryptKey))
            {
                _logger.LogError("缺少加密密钥，无法解密事件数据");
                return null;
            }

            return await _decryptor.DecryptAsync(encryptedData, _providedEncryptKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解密事件数据时发生错误");
            return null;
        }
    }

    /// <summary>
    /// 检查去重状态
    /// </summary>
    private async Task<(bool shouldSkip, bool isProcessing)> CheckDeduplicationAsync(string eventId, string? appKey, CancellationToken cancellationToken)
    {
        if (_distributedDeduplicator != null)
        {
            return (await _distributedDeduplicator.TryMarkAsProcessedAsync(eventId, appKey, cancellationToken: cancellationToken), false);
        }
        else
        {
            return (_deduplicator.TryMarkAsProcessing(eventId), false);
        }
    }

    /// <summary>
    /// 标记去重为已完成
    /// </summary>
    private void MarkDeduplicationCompletedAsync(string eventId)
    {
        _deduplicator.MarkAsCompleted(eventId);
    }

    /// <summary>
    /// 回滚去重状态
    /// </summary>
    private void RollbackDeduplicationAsync(string eventId)
    {
        _deduplicator.RollbackProcessing(eventId);
    }
}