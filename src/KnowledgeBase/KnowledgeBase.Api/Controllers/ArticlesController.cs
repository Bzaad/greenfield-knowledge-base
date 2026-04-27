using FluentValidation;
using KnowledgeBase.Api.DTOs;
using KnowledgeBase.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeBase.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ArticlesController(
    IArticleService articleService,
    IValidator<CreateArticleRequest> validator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ArticleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateArticleRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(
                validation.ToDictionary()));

        var article = await articleService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ArticleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var article = await articleService.GetByIdAsync(id, ct);
        return Ok(article);
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<ArticleResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await articleService.SearchAsync(search, category, page, pageSize, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/upvote")]
    [ProducesResponseType<ArticleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Upvote(Guid id, CancellationToken ct)
    {
        var article = await articleService.UpvoteAsync(id, ct);
        return Ok(article);
    }
}
