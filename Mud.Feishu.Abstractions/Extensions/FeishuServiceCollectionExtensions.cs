// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    private static bool _isConfigured = false;
    private static bool _userTokenManagerAdded = false;
    private static bool _appTokenManagerAdded = false;
    private static bool _isFeishuHttpClient = false;
    private static bool _tenantTokenManagerAdded = false;
    private static bool _tokenManagersAdded = false;

    /// <summary>
    /// 从配置文件读取配置
    /// </summary>
    /// <param name="configuration">配置对象</param>
    /// <param name="sectionName">配置节名称，默认为"Feishu"</param>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection ConfigureFrom(this IServiceCollection services, IConfiguration configuration, string sectionName = "Feishu")
    {
        if (_isConfigured)
            return services;

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var section = sectionName ?? "Feishu";
        services.Configure<FeishuOptions>(options => configuration.GetSection(section).Bind(options));

        _isConfigured = true;
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
        if (_isConfigured)
            return services;
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        services.Configure(configureOptions);
        _isConfigured = true;
        return services;
    }

    /// <summary>
    /// 添加飞书HttpClient注册代码。
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddFeishuHttpClient<TImplementation>(this IServiceCollection services)
        where TImplementation : class, IEnhancedHttpClient
    {
        if (_isFeishuHttpClient) return services;

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

        services.AddSingleton<IFeishuV3AuthenticationApi, FeishuV3AuthenticationApi>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());
        _isFeishuHttpClient = true;
        return services;
    }

    /// <summary>
    /// 添加飞书HttpClient注册代码。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddFeishuHttpClient(this IServiceCollection services) => services.AddFeishuHttpClient<FeishuHttpClient>();

    /// <summary>
    /// 添加令牌管理服务
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
    /// 添加令牌管理服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合实例。支持链式调用</returns>
    public static IServiceCollection AddTokenManagers(this IServiceCollection services)
    {
        services.AddFeishuHttpClient();
        if (!_tokenManagersAdded)
        {
            services
                .AddSingleton<ITenantTokenManager, TenantTokenManager>()
                .AddSingleton<IAppTokenManager, AppTokenManager>()
                .AddSingleton<IUserTokenManager, UserTokenManager>();

            _tokenManagersAdded = true;
        }
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
        if (!_tenantTokenManagerAdded)
        {
            services.AddSingleton<ITenantTokenManager, TenantTokenManager>();
            _tenantTokenManagerAdded = true;
        }
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
        if (!_appTokenManagerAdded)
        {
            services.AddSingleton<IAppTokenManager, AppTokenManager>();
            _appTokenManagerAdded = true;
        }
        return services;
    }

    /// <summary>
    /// 添加应用令牌管理服务
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
        if (!_userTokenManagerAdded)
        {
            services.AddSingleton<IUserTokenManager, UserTokenManager>();
            _userTokenManagerAdded = true;
        }
        return services;
    }
}
