using FluentValidation;
using KnowledgeBase.Api.Data;
using KnowledgeBase.Api.Infrastructure;
using KnowledgeBase.Api.Services;
using KnowledgeBase.Api.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHttpContextAccessor();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<KnowledgeBaseDbContext>(opts =>
    {
        opts.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
        if (builder.Environment.IsDevelopment())
            opts.EnableSensitiveDataLogging();
    });
}

builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
builder.Services.AddSingleton<ISemanticSearchService, AzureOpenAISemanticSearchService>();
builder.Services.AddHttpClient();

builder.Services.AddValidatorsFromAssemblyContaining<CreateArticleRequestValidator>();

// Azure AD authentication — configure TenantId and ClientId in appsettings.
// When not configured (e.g. local dev), auth is skipped and endpoints are open.
var azureAdSection = builder.Configuration.GetSection("AzureAd");
if (azureAdSection.Exists() && !string.IsNullOrEmpty(azureAdSection["TenantId"]))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.Authority = $"{azureAdSection["Instance"]}{azureAdSection["TenantId"]}/v2.0";
            opts.Audience = azureAdSection["ClientId"];
        });
    builder.Services.AddAuthorization();
}

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Apply EF Core migrations (or create schema) on startup for local development.
    // Production deployments should run migrations as a pre-release step.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Expose Program for TestWebApplicationFactory in integration tests
public partial class Program;
