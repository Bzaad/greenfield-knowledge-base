namespace KnowledgeBase.Api.DTOs;

public record CreateArticleRequest(
    string Title,
    string Body,
    List<string> Tags,
    Guid? CategoryId
);
