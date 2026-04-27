using KnowledgeBase.Api.DTOs;

namespace KnowledgeBase.Api.Services;

public interface IArticleService
{
    Task<ArticleResponse> CreateAsync(CreateArticleRequest request, CancellationToken ct = default);
    Task<ArticleResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ArticleResponse>> SearchAsync(string? searchTerm, string? categoryName, int page, int pageSize, CancellationToken ct = default);
    Task<ArticleResponse> UpvoteAsync(Guid id, CancellationToken ct = default);
}
