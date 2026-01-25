// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.TokenManager;
using Mud.HttpUtils.Attributes;

namespace Mud.Feishu.Abstractions.Tests;

/// <summary>
/// 多应用功能测试
/// </summary>
public class MultiAppTests
{
    // 测试常量
    private const string TestEmptyAppSecret = "";
    private const string TestAppSecret = "test_secret_12345678";
    private const string TestVerySecretKey = "very_secret_key_12345";
    private const string TestSecret1 = "secret1_12345678";
    private const string TestSecret2 = "secret2_12345678";
    private const string TestDefaultSecret = "default_secret_123456";
    private const string TestHrSecret = "hr_secret_123456";
    private const string TestFinanceSecret = "finance_secret_123456";

    [Fact]
    public void MultiApp_ConfigurationValidation_ShouldThrowOnInvalidConfig()
    {
        // Arrange
        var invalidConfig = new FeishuAppConfig
        {
            AppKey = "",
            AppId = "",
            AppSecret = TestEmptyAppSecret
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => invalidConfig.Validate());
    }

    [Fact]
    public void MultiApp_DefaultAppKey_ShouldAutoSetIsDefault()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = "default",
            AppId = "cli_test_app_id_1234567890",
            AppSecret = TestAppSecret,
            IsDefault = false  // 这应该被自动推断为 true
        };

        // Act
        config.Validate();

        // Assert - Validate() 方法应该自动将 IsDefault 设置为 true
        Assert.True(config.IsDefault);
    }

    [Fact]
    public void MultiApp_DefaultAppKey_AutoInference_ShouldWork()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = "default",
            AppId = "cli_test_app_id_1234567890",
            AppSecret = TestAppSecret
        };

        // Act - Validate() 方法应该自动推断
        config.Validate();

        // Assert
        Assert.True(config.IsDefault);
    }

    [Fact]
    public void MultiApp_ValidConfiguration_ShouldPassValidation()
    {
        // Arrange
        var validConfig = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = "cli_test_app_id_1234567890",
            AppSecret = TestAppSecret,
            BaseUrl = "https://open.feishu.cn",
            TimeOut = 30,
            RetryCount = 3,
            RetryDelayMs = 1000,
            TokenRefreshThreshold = 300,
            EnableLogging = true,
            IsDefault = true
        };

        // Act & Assert
        var exception = Record.Exception(() => validConfig.Validate());
        Assert.Null(exception);
    }

    [Fact]
    public void MultiApp_InvalidRetryDelayMs_ShouldThrow()
    {
        // Arrange
        var invalidConfig = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = "cli_test_app_id_1234567890",
            AppSecret = TestAppSecret,
            RetryDelayMs = 50  // 低于最小值 100
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => invalidConfig.Validate());
        Assert.Contains("RetryDelayMs", ex.Message);
    }

    [Fact]
    public void MultiApp_SensitiveDataToString_ShouldMaskSecret()
    {
        // Arrange
        var config = new FeishuAppConfig
        {
            AppKey = "test-app",
            AppId = "cli_test_app_id_1234567890",
            AppSecret = TestVerySecretKey
        };

        // Act
        var configString = config.ToString();

        // Assert
        Assert.Contains("te****45", configString);
        Assert.DoesNotContain("very_secret_key", configString);
    }

    [Fact]
    public void MultiApp_DuplicateAppKeys_ShouldThrowOnRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = "app1",
                AppId = "cli_app1_id_1234567890",
                AppSecret = TestSecret1,
                IsDefault = true
            },
            new FeishuAppConfig
            {
                AppKey = "app1",  // 重复的AppKey
                AppId = "cli_app2_id_1234567890",
                AppSecret = TestSecret2
            }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
                sp,
                configs,
                sp.GetRequiredService<ILogger<FeishuAppManager>>()));
        });
    }

    [Fact]
    public void MultiApp_NoDefaultApp_ShouldThrowOnRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = "app1",
                AppId = "cli_app1_id_1234567890",
                AppSecret = "secret1_12345678",
                IsDefault = false
            }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
                sp,
                configs,
                sp.GetRequiredService<ILogger<FeishuAppManager>>()));
        });
    }

    [Fact]
    public void MultiApp_GetApp_ShouldReturnCorrectApp()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = "default",
                AppId = "cli_default_id_1234567890",
                AppSecret = TestDefaultSecret,
                IsDefault = true
            },
            new FeishuAppConfig
            {
                AppKey = "hr-app",
                AppId = "cli_hr_id_1234567890",
                AppSecret = TestHrSecret
            }
        };

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act
        var hrApp = appManager.GetApp("hr-app");

        // Assert
        Assert.NotNull(hrApp);
        Assert.Equal("hr-app", hrApp.Config.AppKey);
        Assert.Equal("cli_hr_id_1234567890", hrApp.Config.AppId);
    }

    [Fact]
    public void MultiApp_GetNonExistentApp_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = "default",
                AppId = "cli_default_id_1234567890",
                AppSecret = "default_secret_123456",
                IsDefault = true
            }
        };

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => appManager.GetApp("non-existent-app"));
    }

    [Fact]
    public void MultiApp_GetDefaultApp_ShouldReturnDefaultApp()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = "default",
                AppId = "cli_default_id_1234567890",
                AppSecret = "default_secret_123456",
                IsDefault = true
            },
            new FeishuAppConfig
            {
                AppKey = "hr-app",
                AppId = "cli_hr_id_1234567890",
                AppSecret = "hr_secret_123456"
            }
        };

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act
        var defaultApp = appManager.GetDefaultApp();

        // Assert
        Assert.NotNull(defaultApp);
        Assert.True(defaultApp.Config.IsDefault);
        Assert.Equal("default", defaultApp.Config.AppKey);
    }

    [Fact]
    public void MultiApp_GetAllApps_ShouldReturnAllRegisteredApps()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = "default",
                AppId = "cli_default_id_1234567890",
                AppSecret = "default_secret_123456",
                IsDefault = true
            },
            new FeishuAppConfig
            {
                AppKey = "hr-app",
                AppId = "cli_hr_id_1234567890",
                AppSecret = "hr_secret_123456"
            },
            new FeishuAppConfig
            {
                AppKey = "finance-app",
                AppId = "cli_finance_id_1234567890",
                AppSecret = "finance_secret_123456"
            }
        };

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act
        var allApps = appManager.GetAllApps();

        // Assert
        Assert.Equal(3, allApps.Count());
        Assert.Contains(allApps, app => app.Config.AppKey == "default");
        Assert.Contains(allApps, app => app.Config.AppKey == "hr-app");
        Assert.Contains(allApps, app => app.Config.AppKey == "finance-app");
    }

    [Fact]
    public void MultiApp_HasApp_ShouldReturnCorrectStatus()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = "default",
                AppId = "cli_default_id_1234567890",
                AppSecret = "default_secret_123456",
                IsDefault = true
            }
        };

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act & Assert
        Assert.True(appManager.HasApp("default"));
        Assert.False(appManager.HasApp("non-existent"));
    }

    [Fact]
    public void MultiApp_AppContextProperties_ShouldBeIsolated()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = "default",
                AppId = "cli_default_id_1234567890",
                AppSecret = "default_secret_123456",
                IsDefault = true
            },
            new FeishuAppConfig
            {
                AppKey = "hr-app",
                AppId = "cli_hr_id_1234567890",
                AppSecret = "hr_secret_123456"
            }
        };

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act
        var defaultApp = appManager.GetApp("default");
        var hrApp = appManager.GetApp("hr-app");

        // Assert
        Assert.NotNull(defaultApp.GetTokenManager(TokenType.TenantAccessToken));
        Assert.NotNull(defaultApp.GetTokenManager(TokenType.AppAccessToken));
        Assert.NotNull(defaultApp.GetTokenManager(TokenType.UserAccessToken));
        Assert.NotNull(defaultApp.Authentication);
        Assert.NotNull(defaultApp.HttpClient);

        // 验证不同应用的配置隔离
        Assert.Equal("cli_default_id_1234567890", defaultApp.Config.AppId);
        Assert.Equal("cli_hr_id_1234567890", hrApp.Config.AppId);

        // 验证HttpClient隔离
        Assert.NotEqual(defaultApp.HttpClient.GetHashCode(), hrApp.HttpClient.GetHashCode());
    }

    [Fact]
    public void MultiApp_RemoveDefaultApp_ShouldFailWhenMultipleApps()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddSingleton<IFeishuAuthentication, FeishuV3Authentication>();
        services.Configure<JsonSerializerOptions>(options => HttpClientExtensions.GetDefaultJsonSerializerOptions());

        var configs = new List<FeishuAppConfig>
        {
            new FeishuAppConfig
            {
                AppKey = "default",
                AppId = "cli_default_id_1234567890",
                AppSecret = "default_secret_123456",
                IsDefault = true
            },
            new FeishuAppConfig
            {
                AppKey = "hr-app",
                AppId = "cli_hr_id_1234567890",
                AppSecret = "hr_secret_123456"
            }
        };

        services.AddSingleton<ITokenCache, MemoryTokenCache>();
        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => appManager.RemoveApp("default"));
    }
}
