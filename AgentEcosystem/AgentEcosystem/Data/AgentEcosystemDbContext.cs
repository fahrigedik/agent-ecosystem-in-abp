using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using AgentEcosystem.Entities;

namespace AgentEcosystem.Data;

public class AgentEcosystemDbContext : AbpDbContext<AgentEcosystemDbContext>
{
    /// <summary>
    /// Araştırma kayıtları tablosu.
    /// </summary>
    public DbSet<ResearchRecord> ResearchRecords { get; set; }
    
    public const string DbTablePrefix = "App";
    public const string DbSchema = null;

    public AgentEcosystemDbContext(DbContextOptions<AgentEcosystemDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigurePermissionManagement();
        builder.ConfigureBlobStoring();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        
        /* Configure your own entities here */
        builder.Entity<ResearchRecord>(b =>
        {
            b.ToTable(DbTablePrefix + "ResearchRecords", DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Query).IsRequired().HasMaxLength(1000);
            b.Property(x => x.RawData).HasMaxLength(int.MaxValue);
            b.Property(x => x.AnalyzedResult).HasMaxLength(int.MaxValue);
            b.Property(x => x.Sources).HasMaxLength(int.MaxValue);
            b.Property(x => x.SessionId).HasMaxLength(100);

            b.HasIndex(x => x.Query);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.CompletedAt);
        });
    }
}

