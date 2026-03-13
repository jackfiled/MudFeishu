using Microsoft.EntityFrameworkCore;
using FeishuFileServer.Models;

namespace FeishuFileServer.Data;

public class FeishuFileDbContext : DbContext
{
    public FeishuFileDbContext(DbContextOptions<FeishuFileDbContext> options) : base(options)
    {
    }

    public DbSet<FileRecord> FileRecords { get; set; }
    public DbSet<FolderRecord> FolderRecords { get; set; }
    public DbSet<VersionRecord> VersionRecords { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FileRecord>(entity =>
        {
            entity.HasIndex(e => e.FileToken).IsUnique();
            entity.HasIndex(e => e.FolderToken);
            entity.HasIndex(e => e.FileMD5);
            entity.HasIndex(e => e.IsDeleted);
        });

        modelBuilder.Entity<FolderRecord>(entity =>
        {
            entity.HasIndex(e => e.FolderToken).IsUnique();
            entity.HasIndex(e => e.ParentFolderToken);
            entity.HasIndex(e => e.IsDeleted);
        });

        modelBuilder.Entity<VersionRecord>(entity =>
        {
            entity.HasIndex(e => e.FileToken);
            entity.HasIndex(e => e.VersionToken).IsUnique();
            entity.HasIndex(e => e.IsCurrentVersion);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.IsActive);
        });
    }
}
