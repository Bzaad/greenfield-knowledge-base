namespace KnowledgeBase.Api.Services;

// Stub implementation showing the integration pattern with Azure OpenAI.
//
// Production flow:
//   1. IndexArticleAsync: call Azure OpenAI /embeddings with title + body excerpt,
//      store the resulting float[] vector in Azure AI Search alongside the article ID.
//   2. FindSimilarArticlesAsync: embed the query, run a vector similarity search
//      in Azure AI Search, return ranked article IDs for hybrid re-ranking with
//      the keyword results from SQL.
public class AzureOpenAISemanticSearchService : ISemanticSearchService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AzureOpenAISemanticSearchService> _logger;

    public AzureOpenAISemanticSearchService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<AzureOpenAISemanticSearchService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Guid>> FindSimilarArticlesAsync(string query, int maxResults, CancellationToken ct = default)
    {
        // TODO: embed query → vector search in Azure AI Search → return ranked IDs
        _logger.LogDebug("Semantic search called for query '{Query}' (stub — returning empty)", query);
        await Task.CompletedTask;
        return [];
    }

    public async Task IndexArticleAsync(Guid articleId, string title, string body, CancellationToken ct = default)
    {
        // TODO: truncate body to ~8,000 tokens, call Azure OpenAI /embeddings,
        // upsert document in Azure AI Search index with articleId + vector
        _logger.LogDebug("Indexing article {ArticleId} for semantic search (stub — no-op)", articleId);
        await Task.CompletedTask;
    }
}
