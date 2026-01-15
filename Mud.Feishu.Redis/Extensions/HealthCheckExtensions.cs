// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mud.Feishu.Redis.HealthChecks;

namespace Mud.Feishu.Redis.Extensions;

/// <summary>
/// 健康检查扩展方法
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// 添加 Redis 健康检查
    /// </summary>
    /// <param name="builder">健康检查构建器</param>
    /// <param name="name">健康检查名称，默认为"redis"</param>
    /// <param name="failureStatus">失败状态，默认为 Unhealthy</param>
    /// <param name="tags">标签集合</param>
    /// <returns>健康检查构建器</returns>
    public static IHealthChecksBuilder AddFeishuRedisHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "redis",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        return builder.AddCheck<RedisHealthCheck>(
            name: name ?? "redis",
            failureStatus: failureStatus,
            tags: tags ?? Array.Empty<string>());
    }
}
