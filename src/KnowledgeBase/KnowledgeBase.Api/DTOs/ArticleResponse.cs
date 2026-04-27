namespace KnowledgeBase.Api.DTOs;

public record ArticleResponse(
    Guid Id,
    string Title,
    string Body,
    IReadOnlyList<string> Tags,
    CategorySummary Category,
    string AuthorId,
    string AuthorName,
    int ViewCount,
    int UpvoteCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record CategorySummary(Guid Id, string Name);
