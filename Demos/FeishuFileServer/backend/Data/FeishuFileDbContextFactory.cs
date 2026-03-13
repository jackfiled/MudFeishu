using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FeishuFileServer.Data;

public class FeishuFileDbContextFactory : IDesignTimeDbContextFactory<FeishuFileDbContext>
{
    public FeishuFileDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FeishuFileDbContext>();
        optionsBuilder.UseSqlite("Data Source=FeishuFile.db");

        return new FeishuFileDbContext(optionsBuilder.Options);
    }
}
