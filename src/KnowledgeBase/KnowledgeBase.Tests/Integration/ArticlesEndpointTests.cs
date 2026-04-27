using System.Net;
using System.Net.Http.Json;
using KnowledgeBase.Api.DTOs;
using KnowledgeBase.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Tests.Integration;

public class ArticlesEndpointTests(TestWebApplicationFactory factory)
    : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task POST_Articles_ValidRequest_Returns201WithLocation()
    {
        var request = new CreateArticleRequest(
            "Integration Test Article",
            "This is a body long enough to pass validation.",
            ["testing", "integration"],
            null);

        var response = await _client.PostAsJsonAsync("/api/articles", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var article = await response.Content.ReadFromJsonAsync<ArticleResponse>();
        Assert.NotNull(article);
        Assert.Equal(request.Title, article.Title);
        Assert.Equal(TestWebApplicationFactory.TestUserName, article.AuthorName);
        Assert.Contains("testing", article.Tags);
    }

    [Fact]
    public async Task POST_Articles_EmptyTitle_Returns400WithValidationErrors()
    {
        var request = new CreateArticleRequest("", "Body content here.", [], null);

        var response = await _client.PostAsJsonAsync("/api/articles", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("Title"));
    }
}
