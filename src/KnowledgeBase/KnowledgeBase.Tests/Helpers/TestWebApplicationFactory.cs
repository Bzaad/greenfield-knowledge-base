using System.Security.Claims;
using System.Text.Encodings.Web;
using KnowledgeBase.Api.Data;
using KnowledgeBase.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeBase.Tests.Helpers;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestUserId = "test-user-001";
    public const string TestUserName = "Test User";
    private readonly string _dbName = $"TestDb-{Guid.NewGuid()}";
    private readonly SqliteConnection _anchor;

    public TestWebApplicationFactory()
    {
        _anchor = new SqliteConnection($"DataSource={_dbName};Mode=Memory;Cache=Shared");
        _anchor.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.AddDbContext<KnowledgeBaseDbContext>(opts =>
                opts.UseSqlite($"DataSource={_dbName};Mode=Memory;Cache=Shared"));

            services.RemoveAll<ICurrentUserService>();
            services.AddScoped<ICurrentUserService>(_ => new StubCurrentUserService(TestUserId, TestUserName));

            services.RemoveAll<ISemanticSearchService>();
            services.AddSingleton<ISemanticSearchService, NullSemanticSearchService>();

            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Apply migrations / seed data (HasData in OnModelCreating)
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseDbContext>();
        db.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _anchor.Dispose();
        base.Dispose(disposing);
    }
}

public class StubCurrentUserService(string userId, string displayName) : ICurrentUserService
{
    public string UserId => userId;
    public string DisplayName => displayName;
}

public class NullSemanticSearchService : ISemanticSearchService
{
    public Task<IReadOnlyList<Guid>> FindSimilarArticlesAsync(string query, int maxResults, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Guid>>([]);

    public Task IndexArticleAsync(Guid articleId, string title, string body, CancellationToken ct = default)
        => Task.CompletedTask;
}

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("oid", TestWebApplicationFactory.TestUserId),
            new Claim("name", TestWebApplicationFactory.TestUserName)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
