namespace KnowledgeBase.Api.Entities;

public class Upvote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
}
