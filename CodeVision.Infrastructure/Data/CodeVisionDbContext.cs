using Microsoft.EntityFrameworkCore;
using CodeVision.Core.Entities;

namespace CodeVision.Infrastructure.Data;

public class CodeVisionDbContext : DbContext
{
    public CodeVisionDbContext(DbContextOptions<CodeVisionDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Suppress pending model changes warning for Railway deployment
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    public DbSet<PullRequestAnalysis> PullRequestAnalyses { get; set; }
    public DbSet<AnalysisNotification> AnalysisNotifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PullRequestAnalysis configuration
        modelBuilder.Entity<PullRequestAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RepoName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PrTitle).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PrAuthor).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Summary).HasColumnType("text");
            entity.Property(e => e.RoslynFindings).HasColumnType("jsonb");
            entity.Property(e => e.GptSuggestions).HasColumnType("jsonb");
            entity.Property(e => e.RawDiff).HasColumnType("text");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.ProcessedAt).IsRequired();

            // İndeksler
            entity.HasIndex(e => new { e.RepoName, e.PrNumber }).IsUnique();
            entity.HasIndex(e => e.QualityScore);
            entity.HasIndex(e => e.RiskLevel);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ProcessedAt);
        });

        // AnalysisNotification configuration
        modelBuilder.Entity<AnalysisNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).IsRequired();

            // Foreign key relationship
            entity.HasOne(e => e.PullRequestAnalysis)
                  .WithMany(p => p.Notifications)
                  .HasForeignKey(e => e.PullRequestAnalysisId)
                  .OnDelete(DeleteBehavior.Cascade);

            // İndeksler
            entity.HasIndex(e => e.PullRequestAnalysisId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsRead);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is PullRequestAnalysis && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (PullRequestAnalysis)entityEntry.Entity;
            
            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
