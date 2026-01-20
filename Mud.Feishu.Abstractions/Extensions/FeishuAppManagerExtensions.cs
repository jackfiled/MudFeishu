// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.TokenManager;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书应用管理器便捷扩展方法
/// </summary>
/// <remarks>
/// 提供简化的API来获取不同应用的令牌。
/// 无需先获取应用上下文，直接通过应用键获取令牌。
/// </remarks>
public static class FeishuAppManagerExtensions
{
    /// <summary>
    /// 获取应用的租户令牌（便捷方法）
    /// </summary>
    /// <param name="manager">应用管理器</param>
    /// <param name="appKey">应用键，必须指定</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Bearer格式的租户令牌</returns>
    /// <remarks>
    /// 便捷方法，无需先获取应用上下文即可获取租户令牌。
    /// 示例：
    /// <code>
    /// // 使用默认应用（通过"default"键获取）
    /// var token1 = await appManager.GetTenantTokenAsync("default");
    ///
    /// // 使用指定应用
    /// var token2 = await appManager.GetTenantTokenAsync("hr-app");
    /// </code>
    /// </remarks>
    public static async Task<string?> GetTenantTokenAsync(
        this IFeishuAppManager manager,
        string appKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("必须指定应用键（appKey）", nameof(appKey));

        var app = manager.GetApp(appKey);
        return await app.TenantTokenManager.GetTokenAsync(cancellationToken);
    }

    /// <summary>
    /// 获取应用的应用令牌（便捷方法）
    /// </summary>
    /// <param name="manager">应用管理器</param>
    /// <param name="appKey">应用键，必须指定</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Bearer格式的应用令牌</returns>
    /// <remarks>
    /// 便捷方法，无需先获取应用上下文即可获取应用令牌。
    /// 示例：
    /// <code>
    /// // 使用默认应用（通过"default"键获取）
    /// var token1 = await appManager.GetAppTokenAsync("default");
    ///
    /// // 使用指定应用
    /// var token2 = await appManager.GetAppTokenAsync("hr-app");
    /// </code>
    /// </remarks>
    public static async Task<string?> GetAppTokenAsync(
        this IFeishuAppManager manager,
        string appKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("必须指定应用键（appKey）", nameof(appKey));

        var app = manager.GetApp(appKey);
        return await app.AppTokenManager.GetTokenAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户令牌（便捷方法）
    /// </summary>
    /// <param name="manager">应用管理器</param>
    /// <param name="userId">用户ID</param>
    /// <param name="appKey">应用键，必须指定</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Bearer格式的用户令牌</returns>
    /// <remarks>
    /// 便捷方法，无需先获取应用上下文即可获取用户令牌。
    /// 示例：
    /// <code>
    /// // 使用默认应用（通过"default"键获取）
    /// var token1 = await appManager.GetUserTokenAsync("user123", "default");
    ///
    /// // 使用指定应用
    /// var token2 = await appManager.GetUserTokenAsync("user456", "hr-app");
    /// </code>
    /// </remarks>
    public static async Task<string?> GetUserTokenAsync(
        this IFeishuAppManager manager,
        string userId,
        string appKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("必须指定用户ID（userId）", nameof(userId));
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("必须指定应用键（appKey）", nameof(appKey));

        var app = manager.GetApp(appKey);
        return await app.UserTokenManager.GetTokenAsync(userId, cancellationToken);
    }

    /// <summary>
    /// 使用授权码获取用户令牌（便捷方法）
    /// </summary>
    /// <param name="manager">应用管理器</param>
    /// <param name="code">授权码</param>
    /// <param name="redirectUri">重定向地址</param>
    /// <param name="appKey">应用键，必须指定</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户令牌信息</returns>
    /// <remarks>
    /// 便捷方法，无需先获取应用上下文即可通过授权码获取用户令牌。
    /// </remarks>
    public static async Task<CredentialToken?> GetUserTokenWithCodeAsync(
        this IFeishuAppManager manager,
        string code,
        string redirectUri,
        string appKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("必须指定授权码（code）", nameof(code));
        if (string.IsNullOrWhiteSpace(redirectUri))
            throw new ArgumentException("必须指定重定向地址（redirectUri）", nameof(redirectUri));
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("必须指定应用键（appKey）", nameof(appKey));

        var app = manager.GetApp(appKey);
        return await app.UserTokenManager.GetUserTokenWithCodeAsync(code, redirectUri, cancellationToken);
    }

    /// <summary>
    /// 使用刷新令牌获取新的用户令牌（便捷方法）
    /// </summary>
    /// <param name="manager">应用管理器</param>
    /// <param name="userId">用户ID</param>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="appKey">应用键，必须指定</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新的用户令牌信息</returns>
    /// <remarks>
    /// 便捷方法，无需先获取应用上下文即可刷新用户令牌。
    /// </remarks>
    public static async Task<CredentialToken?> RefreshUserTokenAsync(
        this IFeishuAppManager manager,
        string userId,
        string refreshToken,
        string appKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("必须指定用户ID（userId）", nameof(userId));
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("必须指定刷新令牌（refreshToken）", nameof(refreshToken));
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("必须指定应用键（appKey）", nameof(appKey));

        var app = manager.GetApp(appKey);
        return await app.UserTokenManager.RefreshUserTokenAsync(userId, refreshToken, cancellationToken);
    }

    /// <summary>
    /// 获取应用的缓存统计信息（便捷方法）
    /// </summary>
    /// <param name="manager">应用管理器</param>
    /// <param name="appKey">应用键，必须指定</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含总令牌数和过期令牌数的元组</returns>
    /// <remarks>
    /// 便捷方法，获取指定应用的令牌缓存统计信息。
    /// </remarks>
    public static async Task<(int Total, int Expired)> GetCacheStatisticsAsync(
        this IFeishuAppManager manager,
        string appKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            throw new ArgumentException("必须指定应用键（appKey）", nameof(appKey));

        var app = manager.GetApp(appKey);
        return await app.TenantTokenManager.GetCacheStatisticsAsync();
    }
}
