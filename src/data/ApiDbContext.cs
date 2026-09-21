using data.Entities;
using Microsoft.EntityFrameworkCore;

namespace data;

public class ApiDbContext(DbContextOptions<ApiDbContext> options) : DbContext(options)
{
    public DbSet<JobRecord> Jobs => Set<JobRecord>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Tier> Tiers => Set<Tier>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobRecord>(entity =>
        {
            entity.HasKey(job => job.Id);
            entity.Property(job => job.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(job => job.ApiKeyId);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(key => key.Id);
            entity.HasIndex(key => key.HashedKey).IsUnique();
        });

        modelBuilder.Entity<Tier>(entity => entity.HasKey(tier => tier.Id));

        modelBuilder.Entity<UsageRecord>(entity =>
        {
            entity.HasKey(usage => usage.Id);
            entity.HasIndex(usage => new { usage.ApiKeyId, usage.RecordedAt });
        });
    }
}