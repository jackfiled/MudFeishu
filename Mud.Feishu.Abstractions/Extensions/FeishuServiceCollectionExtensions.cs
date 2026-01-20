// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Internal;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.TokenManager;
using Polly;
using System.Net;
using System.Text.Json;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书服务集合扩展方法
/// </summary>
public static class FeishuServiceCollectionExtensions
{
    /// <summary>
    /// 从配置文件读取配置
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection ConfigureFrom(this IServiceCollection services, IConfiguration configuration, string sectionName = "Feishu")
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var section = sectionName ?? "Feishu";
        services.Configure<FeishuOptions>(options => configuration.GetSection(section).Bind(options));

        return services;
    }

    /// <summary>
    /// 使用代码配置
    /// </summary>
    /// <param name="configureOptions">配置选项的委托</param>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection ConfigureOptions(this IServiceCollection services, Action<FeishuOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        services.Configure(configureOptions);
        return services;
    }

    /// <summary>
    /// 添加飞书HttpClient注册代码。
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddFeishuHttpClient<TImplementation>(this IServiceCollection services)
        where TImplementation : class, IEnhancedHttpClient
    {
        services.AddHttpClient<IEnhancedHttpClient, TImplementation>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<FeishuOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl ?? "https://open.feishu.cn");
            client.DefaultRequestHeaders.Add("User-Agent", "MudFeishuClient/1.0");
            client.Timeout = TimeSpan.FromSeconds(options.TimeOut);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        }).AddTransientHttpErrorPolicy(builder =>
        {
            return builder.WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        });

        services.AddSingleton<IFeishuV3Authentication, FeishuV3Authentication>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());
        return services;
    }

    /// <summary>
    /// 添加飞书HttpClient注册代码。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddFeishuHttpClient(this IServiceCollection services) => services.AddFeishuHttpClient<FeishuHttpClient>();

    /// <summary>
    /// 添加令牌管理服务（使用默认的内存缓存）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection AddTokenManagers(this IServiceCollection services, IConfiguration configuration, string sectionName = "Feishu")
    {
        return services.ConfigureFrom(configuration, sectionName)
                       .AddTokenManagers();
    }

    /// <summary>
    /// 添加令牌管理服务（使用默认的内存缓存）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection AddTokenManagers(this IServiceCollection services)
    {
        services.AddFeishuHttpClient();
        services.TryAddSingleton<ITokenCache, MemoryTokenCache>();

        services.TryAddSingleton<ITenantTokenManager, TenantTokenManager>();
        services.TryAddSingleton<IAppTokenManager, AppTokenManager>();
        services.TryAddSingleton<IUserTokenManager, UserTokenManager>();

        return services;
    }

    /// <summary>
    /// 添加令牌管理服务（使用自定义缓存实现）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection AddTokenManagers<TCacheImplementation>(this IServiceCollection services, IConfiguration configuration, string sectionName = "Feishu")
        where TCacheImplementation : class, ITokenCache
    {
        return services.ConfigureFrom(configuration, sectionName)
                       .AddTokenManagers<TCacheImplementation>();
    }

    /// <summary>
    /// 添加令牌管理服务（使用自定义缓存实现）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection AddTokenManagers<TCacheImplementation>(this IServiceCollection services)
        where TCacheImplementation : class, ITokenCache
    {
        services.AddFeishuHttpClient();
        services.TryAddSingleton<ITokenCache, TCacheImplementation>();

        services.TryAddSingleton<ITenantTokenManager, TenantTokenManager>();
        services.TryAddSingleton<IAppTokenManager, AppTokenManager>();
        services.TryAddSingleton<IUserTokenManager, UserTokenManager>();

        return services;
    }

    /// <summary>
    /// 添加租户令牌管理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection AddTenantTokenManager(this IServiceCollection services, IConfiguration configuration, string sectionName = "Feishu")
    {
        return services.ConfigureFrom(configuration, sectionName)
                       .AddTenantTokenManager();
    }

    /// <summary>
    /// 添加租户令牌管理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection AddTenantTokenManager(this IServiceCollection services)
    {
        services.AddFeishuHttpClient();
        services.TryAddSingleton<ITokenCache, MemoryTokenCache>();
        services.TryAddSingleton<ITenantTokenManager, TenantTokenManager>();
        return services;
    }

    /// <summary>
    /// 添加应用令牌管理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection AddAppTokenManager(this IServiceCollection services, IConfiguration configuration, string sectionName = "Feishu")
    {
        return services.ConfigureFrom(configuration, sectionName)
                       .AddAppTokenManager();
    }

    /// <summary>
    /// 添加应用令牌管理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection AddAppTokenManager(this IServiceCollection services)
    {
        services.AddFeishuHttpClient();
        services.TryAddSingleton<ITokenCache, MemoryTokenCache>();
        services.TryAddSingleton<IAppTokenManager, AppTokenManager>();
        return services;
    }

    /// <summary>
    /// 添加用户令牌管理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection AddUserTokenManager(this IServiceCollection services, IConfiguration configuration, string sectionName = "Feishu")
    {
        return services.ConfigureFrom(configuration, sectionName)
                       .AddUserTokenManager();
    }

    /// <summary>
    /// 添加用户令牌管理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection AddUserTokenManager(this IServiceCollection services)
    {
        services.AddFeishuHttpClient();
        services.TryAddSingleton<ITokenCache, MemoryTokenCache>();
        services.TryAddSingleton<IUserTokenManager, UserTokenManager>();
        return services;
    }

    /// <summary>
    /// 添加令牌缓存服务（自定义实现）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection AddTokenCache<TCacheImplementation>(this IServiceCollection services)
        where TCacheImplementation : class, ITokenCache
    {
        services.TryAddSingleton<ITokenCache, TCacheImplementation>();
        return services;
    }

    /// <summary>
    /// 注册多应用所需的基础服务（内部使用）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    /// <remarks>
    /// 此方法用于多应用系统，注册了基础依赖项但不注册全局TokenManager。
    /// </remarks>
    internal static IServiceCollection AddFeishuMultiAppBaseServices(this IServiceCollection services)
    {
        // 注册IHttpClientFactory，用于为每个应用创建独立的HttpClient
        services.AddHttpClient();

        // 注册认证服务和JSON配置
        services.AddSingleton<IFeishuV3Authentication, FeishuV3Authentication>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

        return services;
    }
}
