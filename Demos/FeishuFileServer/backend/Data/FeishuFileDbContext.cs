using FeishuFileServer.Models;
using Microsoft.EntityFrameworkCore;

namespace FeishuFileServer.Data;

/// <summary>
/// 飞书文件服务数据库上下文
/// 提供对文件、文件夹、版本和用户数据的访问
/// </summary>
public class FeishuFileDbContext : DbContext
{
    /// <summary>
    /// 初始化数据库上下文实例
    /// </summary>
    /// <param name="options">数据库上下文配置选项</param>
    public FeishuFileDbContext(DbContextOptions<FeishuFileDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// 文件记录数据集
    /// </summary>
    public DbSet<FileRecord> FileRecords { get; set; }

    /// <summary>
    /// 文件夹记录数据集
    /// </summary>
    public DbSet<FolderRecord> FolderRecords { get; set; }

    /// <summary>
    /// 版本记录数据集
    /// </summary>
    public DbSet<VersionRecord> VersionRecords { get; set; }

    /// <summary>
    /// 用户数据集
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// 操作日志数据集
    /// </summary>
    public DbSet<OperationLog> OperationLogs { get; set; }

    /// <summary>
    /// 配置数据库模型
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FileRecord>(entity =>
        {
            entity.HasIndex(e => e.FileToken).IsUnique();
            entity.HasIndex(e => e.FolderToken);
            entity.HasIndex(e => e.FileMD5);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<FolderRecord>(entity =>
        {
            entity.HasIndex(e => e.FolderToken).IsUnique();
            entity.HasIndex(e => e.ParentFolderToken);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<VersionRecord>(entity =>
        {
            entity.HasIndex(e => e.VersionToken).IsUnique();
            entity.HasIndex(e => e.FileToken);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email);
        });

        modelBuilder.Entity<OperationLog>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.OperationType);
            entity.HasIndex(e => e.ResourceToken);
            entity.HasIndex(e => e.OperationTime);
        });
    }
}
