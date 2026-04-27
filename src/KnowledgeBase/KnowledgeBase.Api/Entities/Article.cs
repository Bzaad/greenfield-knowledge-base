namespace KnowledgeBase.Api.Entities;

public class Article
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
    public string AuthorName { get; set; } = default!;
    public int ViewCount { get; set; }
    public int UpvoteCount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ArticleTag> ArticleTags { get; set; } = [];
    public ICollection<Upvote> Upvotes { get; set; } = [];
}
