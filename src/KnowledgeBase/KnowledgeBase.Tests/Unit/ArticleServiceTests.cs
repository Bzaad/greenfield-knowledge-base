using KnowledgeBase.Api.Data;
using KnowledgeBase.Api.DTOs;
using KnowledgeBase.Api.Exceptions;
using KnowledgeBase.Api.Services;
using KnowledgeBase.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
namespace KnowledgeBase.Tests.Unit;

public class ArticleServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly KnowledgeBaseDbContext _db;
    private readonly ArticleService _sut;

    public ArticleServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<KnowledgeBaseDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new KnowledgeBaseDbContext(options);
        _db.Database.EnsureCreated();

        var currentUser = new StubCurrentUserService("user-123", "Test Author");
        var semanticSearch = new NullSemanticSearchService();

        _sut = new ArticleService(_db, currentUser, semanticSearch, NullLogger<ArticleService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsArticleWithNormalisedTags()
    {
        var request = new CreateArticleRequest(
            Title: "Client Relationship Management Strategies",
            Body: "Maintaining trust with difficult stakeholders requires active listening and clear escalation paths.",
            Tags: ["stakeholders", "  Relationships  ", "stakeholders"], // duplicates + whitespace
            CategoryId: null);

        var result = await _sut.CreateAsync(request);

        Assert.Equal(request.Title, result.Title);
        Assert.Equal("Test Author", result.AuthorName);

        // Tags should be deduplicated and normalised to lowercase
        Assert.Equal(2, result.Tags.Count);
        Assert.Contains("stakeholders", result.Tags);
        Assert.Contains("relationships", result.Tags);
    }

    [Fact]
    public async Task UpvoteAsync_SameUserVotesTwice_ThrowsConflictException()
    {
        var article = await _sut.CreateAsync(new CreateArticleRequest(
            "Upvote Test Article",
            "Content that is long enough for the validator.",
            [],
            null));

        await _sut.UpvoteAsync(article.Id);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.UpvoteAsync(article.Id));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingArticle_IncrementsViewCount()
    {
        var created = await _sut.CreateAsync(new CreateArticleRequest(
            "View Count Test",
            "Content that is long enough for the validator.",
            [],
            null));

        var fetched = await _sut.GetByIdAsync(created.Id);

        Assert.Equal(1, fetched.ViewCount);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
