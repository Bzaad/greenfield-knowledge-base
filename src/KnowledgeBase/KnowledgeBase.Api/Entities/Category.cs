namespace KnowledgeBase.Api.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public ICollection<Article> Articles { get; set; } = [];
}
