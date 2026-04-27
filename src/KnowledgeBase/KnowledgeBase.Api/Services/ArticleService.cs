using KnowledgeBase.Api.Data;
using KnowledgeBase.Api.DTOs;
using KnowledgeBase.Api.Entities;
using KnowledgeBase.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Api.Services;

public class ArticleService(
    KnowledgeBaseDbContext db,
    ICurrentUserService currentUser,
    ISemanticSearchService semanticSearch,
    ILogger<ArticleService> logger) : IArticleService
{
    private static readonly Guid DefaultCategoryId = new("10000000-0000-0000-0000-000000000005");

    public async Task<ArticleResponse> CreateAsync(CreateArticleRequest request, CancellationToken ct = default)
    {
        var categoryId = request.CategoryId ?? DefaultCategoryId;
        var categoryExists = await db.Categories.AnyAsync(c => c.Id == categoryId, ct);
        if (!categoryExists)
            throw new NotFoundException($"Category '{categoryId}' was not found.");

        var tags = await ResolveTagsAsync(request.Tags, ct);

        var article = new Article
        {
            Title = request.Title,
            Body = request.Body,
            CategoryId = categoryId,
            AuthorId = currentUser.UserId,
            AuthorName = currentUser.DisplayName,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            ArticleTags = tags.Select(t => new ArticleTag { TagId = t.Id }).ToList()
        };

        db.Articles.Add(article);

        await db.SaveChangesAsync(ct);

        // Embed the article for semantic search. Errors here must not fail the create request.
        // A background reindex job can catch missed articles.
        _ = semanticSearch.IndexArticleAsync(article.Id, article.Title, article.Body)
            .ContinueWith(t => logger.LogWarning(t.Exception, "Failed to index article {Id}", article.Id),
                TaskContinuationOptions.OnlyOnFaulted);

        logger.LogInformation("Article {Id} created by {AuthorId}", article.Id, article.AuthorId);

        // Use the internal loader, creating an article is not a view event.
        return await FetchArticleResponseAsync(article.Id, ct);
    }

    public async Task<ArticleResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // AsNoTracking so the entity from the identity map can't shadow the post-ExecuteUpdate DB value.
        var article = await db.Articles
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw NotFoundException.For<Article>(id);

        // Atomic increment avoids a read-modify-write race under concurrent requests.
        await db.Articles
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ViewCount, a => a.ViewCount + 1), ct);

        // Reflect the increment we just applied in the DB so the response is accurate.
        article.ViewCount++;
        return MapToResponse(article);
    }

    public async Task<PagedResult<ArticleResponse>> SearchAsync(
        string? searchTerm,
        string? categoryName,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = db.Articles
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(categoryName))
            query = query.Where(a => a.Category.Name == categoryName);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // Keyword search across title and body. With SQL Server in production,
            // replace this with CONTAINS / FREETEXT for proper full-text indexing.
            var term = searchTerm.ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(term) ||
                a.Body.ToLower().Contains(term));
        }

        // Rank by quality signals so the most trusted content surfaces first.
        query = query.OrderByDescending(a => a.UpvoteCount)
                     .ThenByDescending(a => a.ViewCount)
                     .ThenByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ArticleResponse>(
            items.Select(MapToResponse).ToList(),
            page,
            pageSize,
            totalCount);
    }

    public async Task<ArticleResponse> UpvoteAsync(Guid id, CancellationToken ct = default)
    {
        var exists = await db.Articles.AnyAsync(a => a.Id == id, ct);
        if (!exists)
            throw NotFoundException.For<Article>(id);

        var userId = currentUser.UserId;
        var alreadyVoted = await db.Upvotes.AnyAsync(u => u.ArticleId == id && u.UserId == userId, ct);
        if (alreadyVoted)
            throw new ConflictException("You have already upvoted this article.");

        db.Upvotes.Add(new Upvote { ArticleId = id, UserId = userId, CreatedAt = DateTimeOffset.UtcNow });

        await db.Articles
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.UpvoteCount, a => a.UpvoteCount + 1), ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Article {Id} upvoted by {UserId}", id, userId);

        // Use the internal loader — upvoting is not a view event.
        return await FetchArticleResponseAsync(id, ct);
    }

    // Loads the article DTO without incrementing view count. Used by CreateAsync and UpvoteAsync
    // so that internal operations don't pollute the view count metric.
    private async Task<ArticleResponse> FetchArticleResponseAsync(Guid id, CancellationToken ct)
    {
        var article = await db.Articles
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw NotFoundException.For<Article>(id);

        return MapToResponse(article);
    }

    private async Task<List<Tag>> ResolveTagsAsync(IEnumerable<string> tagNames, CancellationToken ct)
    {
        var normalised = tagNames.Select(t => t.Trim().ToLowerInvariant()).Distinct().ToList();
        var existing = await db.Tags.Where(t => normalised.Contains(t.Name)).ToListAsync(ct);
        var existingNames = existing.Select(t => t.Name).ToHashSet();

        var newTags = normalised
            .Where(n => !existingNames.Contains(n))
            .Select(n => new Tag { Name = n })
            .ToList();

        if (newTags.Count > 0)
            db.Tags.AddRange(newTags);

        return [.. existing, .. newTags];
    }

    private static ArticleResponse MapToResponse(Article a) => new(
        a.Id,
        a.Title,
        a.Body,
        a.ArticleTags.Select(at => at.Tag.Name).ToList(),
        new CategorySummary(a.Category.Id, a.Category.Name),
        a.AuthorId,
        a.AuthorName,
        a.ViewCount,
        a.UpvoteCount,
        a.CreatedAt,
        a.UpdatedAt
    );
}
