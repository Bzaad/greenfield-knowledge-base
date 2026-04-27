namespace KnowledgeBase.Api.Entities;

public class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public ICollection<ArticleTag> ArticleTags { get; set; } = [];
}
