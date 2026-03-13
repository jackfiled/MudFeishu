using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FeishuFileServer.Data;

/// <summary>
/// 数据库上下文工厂
/// 用于设计时（如迁移）创建数据库上下文实例
/// </summary>
public class FeishuFileDbContextFactory : IDesignTimeDbContextFactory<FeishuFileDbContext>
{
    /// <summary>
    /// 创建数据库上下文实例
    /// 用于Entity Framework Core的设计时工具（如迁移命令）
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <returns>数据库上下文实例</returns>
    public FeishuFileDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FeishuFileDbContext>();
        optionsBuilder.UseSqlite("Data Source=FeishuFile.db");

        return new FeishuFileDbContext(optionsBuilder.Options);
    }
}
