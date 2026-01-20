// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mud.Feishu.Abstractions;
using Mud.Feishu.TokenManager;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 飞书多应用服务注册扩展
/// </summary>
/// <remarks>
/// 提供多飞书应用支持的服务注册扩展方法。
/// 支持从配置文件加载或通过代码配置两种方式。
/// </remarks>
public static class FeishuMultiAppExtensions
{
    /// <summary>
    /// 注册飞书多应用支持（从配置文件加载）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"FeishuApps"</param>
    /// <returns>服务集合实例，支持链式调用</returns>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当配置无效时抛出</exception>
    /// <remarks>
    /// 从配置文件加载飞书应用配置。
    /// 配置示例：
    /// <code>
    /// {
    ///   "FeishuApps": [
    ///     {
    ///       "AppKey": "default",
    ///       "AppId": "cli_xxx",
    ///       "AppSecret": "dsk_xxx",
    ///       "IsDefault": true
    ///     },
    ///     {
    ///       "AppKey": "hr-app",
    ///       "AppId": "cli_yyy",
    ///       "AppSecret": "dsk_yyy"
    ///     }
    ///   ]
    /// }
    /// </code>
    /// </remarks>
    public static IServiceCollection AddFeishuMultiApp(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "FeishuApps")
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // 注册基础服务（HttpClient工厂、认证服务等）
        services.AddFeishuMultiAppBaseServices();

        // 注册并验证配置
        services.Configure<List<FeishuAppConfig>>(options =>
        {
            var section = configuration.GetSection(sectionName);
            section.Bind(options);

            // 验证并设置默认应用
            ValidateAndSetDefaultApp(options);
        });

        // 注册令牌缓存和应用管理器
        services.TryAddSingleton<ITokenCache, MemoryTokenCache>();
        services.TryAddSingleton<IFeishuAppManager, FeishuAppManager>();

        return services;
    }

    /// <summary>
    /// 注册飞书多应用支持（使用代码配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">配置委托</param>
    /// <returns>服务集合实例，支持链式调用</returns>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当配置无效时抛出</exception>
    /// <remarks>
    /// 通过代码方式配置飞书应用。
    /// 配置示例：
    /// <code>
    /// services.AddFeishuMultiApp(configure =>
    /// {
    ///     config.AddDefaultApp("default", "cli_xxx", "dsk_xxx");
    ///     config.AddApp("hr-app", "cli_yyy", "dsk_yyy", opt =>
    ///     {
    ///         opt.TimeOut = 45;
    ///         opt.RetryCount = 5;
    ///     });
    /// });
    /// </code>
    /// </remarks>
    public static IServiceCollection AddFeishuMultiApp(
        this IServiceCollection services,
        Action<FeishuAppConfigBuilder> configure)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        // 注册基础服务
        services.AddFeishuMultiAppBaseServices();

        var builder = new FeishuAppConfigBuilder();
        configure(builder);
        var configs = builder.Build();

        // 验证并设置默认应用
        ValidateAndSetDefaultApp(configs);

        services.AddSingleton(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        services.TryAddSingleton<ITokenCache, MemoryTokenCache>();

        return services;
    }

    /// <summary>
    /// 注册飞书多应用支持（使用预构建的配置列表）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configs">应用配置列表</param>
    /// <returns>服务集合实例，支持链式调用</returns>
    /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
    /// <exception cref="InvalidOperationException">当配置无效时抛出</exception>
    /// <remarks>
    /// 使用已构建好的配置列表进行注册。
    /// 配置示例：
    /// <code>
    /// var configs = new List&lt;FeishuAppConfig&gt;
    /// {
    ///     new FeishuAppConfig { AppKey = "default", AppId = "cli_xxx", AppSecret = "dsk_xxx", IsDefault = true },
    ///     new FeishuAppConfig { AppKey = "hr-app", AppId = "cli_yyy", AppSecret = "dsk_yyy" }
    /// };
    /// services.AddFeishuMultiApp(configs);
    /// </code>
    /// </remarks>
    public static IServiceCollection AddFeishuMultiApp(
        this IServiceCollection services,
        List<FeishuAppConfig> configs)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configs == null)
            throw new ArgumentNullException(nameof(configs));

        // 注册基础服务
        services.AddFeishuMultiAppBaseServices();

        // 验证并设置默认应用
        ValidateAndSetDefaultApp(configs);

        services.AddSingleton(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        services.TryAddSingleton<ITokenCache, MemoryTokenCache>();

        return services;
    }

    /// <summary>
    /// 验证并设置默认应用
    /// </summary>
    /// <param name="configs">应用配置列表</param>
    /// <exception cref="InvalidOperationException">当配置无效时抛出</exception>
    private static void ValidateAndSetDefaultApp(List<FeishuAppConfig> configs)
    {
        if (configs.Count == 0)
            throw new InvalidOperationException("至少需要配置一个飞书应用");

        // 检查是否有重复的AppKey
        var duplicateAppKeys = configs.GroupBy(c => c.AppKey, StringComparer.OrdinalIgnoreCase)
                                       .Where(g => g.Count() > 1)
                                       .Select(g => g.Key)
                                       .ToList();
        if (duplicateAppKeys.Any())
            throw new InvalidOperationException($"检测到重复的AppKey: {string.Join(", ", duplicateAppKeys)}");

        // 验证所有配置
        foreach (var config in configs)
        {
            config.Validate();
        }

        // 如果没有指定默认应用，设置第一个为默认
        if (!configs.Any(c => c.IsDefault))
        {
            configs[0].IsDefault = true;
        }
    }
}
