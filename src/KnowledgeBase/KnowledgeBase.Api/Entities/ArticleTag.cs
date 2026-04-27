namespace KnowledgeBase.Api.Entities;

public class ArticleTag
{
    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = default!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = default!;
}
