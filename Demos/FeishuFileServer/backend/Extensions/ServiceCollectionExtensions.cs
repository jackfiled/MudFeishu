using System.Text;
using FeishuFileServer.Configuration;
using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Services;
using FeishuFileServer.Services.Feishu;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Interfaces;

namespace FeishuFileServer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Feishu File Server API", Version = "v1" });
        });

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<FeishuFileDbContext>(options =>
            options.UseSqlite(connectionString));

        services.Configure<FeishuSettings>(configuration.GetSection("FeishuSettings"));
        services.Configure<FileUploadSettings>(configuration.GetSection("FileUploadSettings"));
        services.Configure<VersionManagementSettings>(configuration.GetSection("VersionManagement"));
        services.Configure<CorsSettings>(configuration.GetSection("CorsSettings"));
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        // 添加 JWT 认证
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
        if (jwtSettings != null && !string.IsNullOrEmpty(jwtSettings.Secret))
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                        ClockSkew = TimeSpan.Zero
                    };
                });
        }

        services.AddAuthorization();

        // 注册认证服务
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }

    public static IServiceCollection AddFeishuServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册飞书应用基础服务
        services.AddFeishuApp(configuration, "FeishuApps");

        // 注册飞书云盘 API 服务
        services.CreateFeishuServicesBuilder()
                .AddDriveApi()
                .Build();

        services.AddScoped<IFeishuDriveService, FeishuDriveService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IFolderService, FolderService>();
        services.AddScoped<IVersionService, VersionService>();

        return services;
    }

    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection("CorsSettings").Get<CorsSettings>();

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                policy.WithOrigins(corsSettings?.AllowedOrigins?.ToArray() ?? new[] { "http://localhost:3000" })
                    .WithMethods(corsSettings?.AllowedMethods?.ToArray() ?? new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" })
                    .WithHeaders(corsSettings?.AllowedHeaders?.ToArray() ?? new[] { "*" })
                    .AllowCredentials();
            });
        });

        return services;
    }
}
