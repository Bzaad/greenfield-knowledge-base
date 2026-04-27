namespace KnowledgeBase.Api.Services;

public interface ISemanticSearchService
{
    /// <summary>
    /// Returns article IDs ranked by semantic similarity to the query.
    /// Used to supplement keyword results with intent-aware matches.
    /// </summary>
    Task<IReadOnlyList<Guid>> FindSimilarArticlesAsync(string query, int maxResults, CancellationToken ct = default);

    /// <summary>
    /// Generates and stores embeddings for a newly created or updated article.
    /// Called asynchronously after the article is persisted.
    /// </summary>
    Task IndexArticleAsync(Guid articleId, string title, string body, CancellationToken ct = default);
}
