using KnowledgeBase.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KnowledgeBase.Api.Data;

public class KnowledgeBaseDbContext(DbContextOptions<KnowledgeBaseDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
    public DbSet<Upvote> Upvotes => Set<Upvote>();

    // SQLite doesn't have native DateTimeOffset sorting support. We convert to binary (UTC ticks) to ensure
    // consistent sorting and comparison across both local development (SQLite) and production (SQL Server).
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArticleTag>().HasKey(at => new { at.ArticleId, at.TagId });

        modelBuilder.Entity<Upvote>()
            .HasIndex(u => new { u.ArticleId, u.UserId })
            .IsUnique();

        // Global soft-delete filter — callers must explicitly IgnoreQueryFilters() to see deleted articles
        modelBuilder.Entity<Article>().HasQueryFilter(a => !a.IsDeleted);

        modelBuilder.Entity<Tag>()
            .HasIndex(t => t.Name)
            .IsUnique();

        modelBuilder.Entity<Article>()
            .HasIndex(a => a.CreatedAt);

        SeedCategories(modelBuilder);
    }

    private static void SeedCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = new Guid("10000000-0000-0000-0000-000000000001"), Name = "Project Management", Description = "Lessons learned, templates, and best practices for project delivery" },
            new Category { Id = new Guid("10000000-0000-0000-0000-000000000002"), Name = "Technical", Description = "Architecture guides, code patterns, and engineering standards" },
            new Category { Id = new Guid("10000000-0000-0000-0000-000000000003"), Name = "Sales", Description = "Proposals, client engagement strategies, and bid templates" },
            new Category { Id = new Guid("10000000-0000-0000-0000-000000000004"), Name = "People & Culture", Description = "HR processes, onboarding, and team practices" },
            new Category { Id = new Guid("10000000-0000-0000-0000-000000000005"), Name = "General", Description = "Uncategorised knowledge articles" }
        );
    }
}
