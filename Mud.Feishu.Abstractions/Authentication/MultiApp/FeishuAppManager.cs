// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.TokenManager;
using System.Text.Json;
using ConfigurationFeishuOptions = Mud.Feishu.Abstractions.FeishuOptions;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书应用管理器实现
/// </summary>
/// <remarks>
/// 负责管理系统中所有飞书应用的创建、获取、移除等操作。
/// 每个应用拥有独立的配置、缓存和TokenManager实例。
/// </remarks>
internal class FeishuAppManager : IFeishuAppManager
{
    /// <summary>
    /// 服务提供者
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 应用上下文字典
    /// </summary>
    private readonly Dictionary<string, FeishuAppContext> _apps;

    /// <summary>
    /// 日志记录器
    /// </summary>
    private readonly ILogger<FeishuAppManager> _logger;

    /// <summary>
    /// 初始化飞书应用管理器
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="configs">应用配置列表</param>
    /// <param name="logger">日志记录器</param>
    /// <exception cref="ArgumentNullException">当任何必需参数为null时抛出</exception>
    public FeishuAppManager(
        IServiceProvider serviceProvider,
        IEnumerable<FeishuAppConfig> configs,
        ILogger<FeishuAppManager> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apps = new Dictionary<string, FeishuAppContext>(StringComparer.OrdinalIgnoreCase);

        // 初始化所有应用
        foreach (var config in configs)
        {
            CreateAppContext(config);
        }

        // 验证必须有默认应用（已在配置加载阶段设置）
        if (!_apps.Values.Any(a => a.Config.IsDefault))
        {
            throw new InvalidOperationException("未配置默认应用");
        }

        _logger.LogInformation("飞书应用管理器初始化完成，共加载 {Count} 个应用", _apps.Count);
    }

    /// <summary>
    /// 更改当前的默认应用
    /// </summary>
    /// <param name="appKey">应用键</param>
    /// <returns>默认应用上下文</returns>
    /// <remarks>
    /// 根据应用键更改当前对应的飞书应用上下文。
    /// 应用键是在配置中定义的唯一标识，如 "default", "hr-app" 等。
    /// </remarks>
    public FeishuAppContext ChangeDefaultApp(string appKey)
    {
        var app = GetApp(appKey);
        // 重置其他应用的默认标记
        foreach (var otherApp in _apps.Values)
        {
            otherApp.Config.IsDefault = false;
        }
        app.Config.IsDefault = true;
        _logger.LogInformation("已更改默认飞书应用为: {AppKey} (AppId: {AppId})",
            app.Config.AppKey, app.Config.AppId);
        return app;
    }

    /// <summary>
    /// 获取默认应用上下文
    /// </summary>
    public FeishuAppContext GetDefaultApp()
    {
        var defaultApp = _apps.Values.FirstOrDefault(a => a.Config.IsDefault)
            ?? throw new InvalidOperationException("未配置默认应用");

        return defaultApp;
    }

    /// <summary>
    /// 获取指定应用上下文
    /// </summary>
    public FeishuAppContext GetApp(string appKey)
    {
        if (string.IsNullOrWhiteSpace(appKey))
            return GetDefaultApp();

        if (_apps.TryGetValue(appKey, out var app))
            return app;

        throw new KeyNotFoundException($"未找到飞书应用: {appKey}");
    }

    /// <summary>
    /// 尝试获取应用上下文
    /// </summary>
    public bool TryGetApp(string appKey, out FeishuAppContext? appContext)
    {
        if (string.IsNullOrWhiteSpace(appKey))
        {
            try
            {
                appContext = GetDefaultApp();
                return true;
            }
            catch
            {
                appContext = null;
                return false;
            }
        }

        return _apps.TryGetValue(appKey, out appContext);
    }

    /// <summary>
    /// 获取所有已注册的应用
    /// </summary>
    public IEnumerable<FeishuAppContext> GetAllApps()
    {
        return _apps.Values;
    }

    /// <summary>
    /// 检查应用是否存在
    /// </summary>
    public bool HasApp(string appKey)
    {
        return _apps.ContainsKey(appKey);
    }

    /// <summary>
    /// 运行时添加应用
    /// </summary>
    public FeishuAppContext AddApp(FeishuAppConfig config)
    {
        config.Validate();

        if (_apps.ContainsKey(config.AppKey))
            throw new InvalidOperationException($"应用 {config.AppKey} 已存在");

        var context = CreateAppContext(config);
        return context;
    }

    /// <summary>
    /// 移除应用
    /// </summary>
    public bool RemoveApp(string appKey)
    {
        if (!_apps.TryGetValue(appKey, out var app))
            return false;

        if (app.Config.IsDefault && _apps.Count > 1)
            throw new InvalidOperationException("不能移除默认应用");

        app.Dispose();
        return _apps.Remove(appKey);
    }

    /// <summary>
    /// 创建应用上下文
    /// </summary>
    private FeishuAppContext CreateAppContext(FeishuAppConfig config)
    {
        config.Validate();

        // 创建带应用键前缀的缓存
        var baseCache = _serviceProvider.GetRequiredService<ITokenCache>();
        var appCache = new PrefixedTokenCache(baseCache, config.AppKey);

        // 创建认证API
        var authenticationApi = _serviceProvider.GetRequiredService<IFeishuV3Authentication>();

        // 创建Logger
        var logger = _serviceProvider.GetRequiredService<ILogger<TokenManagerWithCache>>();

        // 创建TokenManager
        var tenantTokenManager = new TenantTokenManager(
            authenticationApi,
            CreateOptions(config),
            logger,
            appCache);

        var appTokenManager = new AppTokenManager(
            authenticationApi,
            CreateOptions(config),
            logger,
            appCache);

        var userTokenManager = new UserTokenManager(
            authenticationApi,
            CreateOptions(config),
            logger,
            appCache);

        // 为每个应用创建独立的HttpClient
        var httpClient = CreateHttpClient(config);

        var context = new FeishuAppContext(
            config,
            tenantTokenManager,
            appTokenManager,
            userTokenManager,
            authenticationApi,
            appCache,
            httpClient);

        _apps[config.AppKey] = context;

        _logger.LogInformation("创建飞书应用上下文: {AppKey} (AppId: {AppId})",
            config.AppKey, config.AppId);

        return context;
    }

    /// <summary>
    /// 创建独立的HttpClient实例
    /// </summary>
    private IEnhancedHttpClient CreateHttpClient(FeishuAppConfig config)
    {
        var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient($"feishu-{config.AppKey}");
        var logger = _serviceProvider.GetRequiredService<ILogger<FeishuHttpClient>>();
        var jsonSerializerOptions = _serviceProvider.GetRequiredService<IOptionsMonitor<JsonSerializerOptions>>();

        // 配置HttpClient
        httpClient.BaseAddress = new Uri(config.BaseUrl ?? "https://open.feishu.cn");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "MudFeishuClient/1.0");
        httpClient.Timeout = TimeSpan.FromSeconds(config.TimeOut);

        // 创建FeishuHttpClient包装器
        var options = Options.Create(new ConfigurationFeishuOptions
        {
            AppId = config.AppId,
            AppSecret = config.AppSecret,
            BaseUrl = config.BaseUrl,
            TimeOut = config.TimeOut,
            EnableLogging = config.EnableLogging
        });

        return new FeishuHttpClient(httpClient, logger, options, jsonSerializerOptions);
    }

    /// <summary>
    /// 创建配置选项
    /// </summary>
    private IOptions<ConfigurationFeishuOptions> CreateOptions(FeishuAppConfig config)
    {
        var internalOptions = InternalFeishuOptions.FromAppConfig(config);
        var options = new ConfigurationFeishuOptions
        {
            AppId = internalOptions.AppId,
            AppSecret = internalOptions.AppSecret,
            BaseUrl = internalOptions.BaseUrl,
            TimeOut = internalOptions.TimeOut,
            RetryCount = internalOptions.RetryCount,
            TokenRefreshThreshold = internalOptions.TokenRefreshThreshold,
            EnableLogging = internalOptions.EnableLogging
        };
        return Options.Create(options);
    }
}
