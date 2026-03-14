using FeishuFileServer.Data;
using FeishuFileServer.Extensions;
using FeishuFileServer.Middleware;
using FeishuFileServer.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 添加 appsettings.local.json 配置文件（用于本地开发配置）
builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddFeishuServices(builder.Configuration);
builder.Services.AddCorsConfiguration(builder.Configuration);

// 添加飞书应用服务
builder.Services.AddFeishuApp(builder.Configuration, "FeishuApps");

// 注册API服务
builder.Services.CreateFeishuServicesBuilder()
    .AddModules(FeishuModule.Organization)
    .AddModules(FeishuModule.Drive)
    .Build()
    .AddLogging(options => options.AddConsole());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FeishuFileDbContext>();
    await dbContext.Database.MigrateAsync();

    // 初始化管理员用户
    await InitializeAdminUserAsync(dbContext);
}

async Task InitializeAdminUserAsync(FeishuFileDbContext dbContext)
{
    if (!await dbContext.Users.AnyAsync(u => u.Role == UserRole.Admin))
    {
        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            DisplayName = "管理员",
            Email = "admin@example.com",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(adminUser);
        await dbContext.SaveChangesAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Feishu File Server API V1");
    });
}

app.UseGlobalExceptionHandler();

app.UseRateLimiter(100);

app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
