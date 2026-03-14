using System.Text.Json;
using FeishuFileServer.Data;
using FeishuFileServer.Models;
using Microsoft.EntityFrameworkCore;

namespace FeishuFileServer.Services;

/// <summary>
/// 操作日志服务实现
/// 提供操作日志的记录和查询功能
/// </summary>
public class OperationLogService : IOperationLogService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly ILogger<OperationLogService> _logger;

    /// <summary>
    /// 初始化操作日志服务实例
    /// </summary>
    public OperationLogService(FeishuFileDbContext dbContext, ILogger<OperationLogService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 记录操作日志
    /// </summary>
    public async Task LogAsync(int? userId, string? username, string operationType, string resourceType,
        string? resourceToken = null, string? resourceName = null, string? details = null,
        string? ipAddress = null, string? userAgent = null, bool isSuccess = true,
        string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var log = new OperationLog
            {
                UserId = userId,
                Username = username,
                OperationType = operationType,
                ResourceType = resourceType,
                ResourceToken = resourceToken,
                ResourceName = resourceName,
                Details = details,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                OperationTime = DateTime.UtcNow
            };

            _dbContext.OperationLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Operation logged: {OperationType} on {ResourceType} by user {UserId}",
                operationType, resourceType, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log operation: {OperationType}", operationType);
        }
    }

    /// <summary>
    /// 获取用户操作日志
    /// </summary>
    public async Task<List<OperationLog>> GetUserLogsAsync(int userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OperationLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.OperationTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取资源操作日志
    /// </summary>
    public async Task<List<OperationLog>> GetResourceLogsAsync(string resourceToken, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OperationLogs
            .Where(l => l.ResourceToken == resourceToken)
            .OrderByDescending(l => l.OperationTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
