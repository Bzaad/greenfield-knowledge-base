using FluentValidation;
using KnowledgeBase.Api.DTOs;

namespace KnowledgeBase.Api.Validators;

public class CreateArticleRequestValidator : AbstractValidator<CreateArticleRequest>
{
    public CreateArticleRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .MinimumLength(10).WithMessage("Body must be at least 10 characters.");

        RuleFor(x => x.Tags)
            .Must(t => t.Count <= 10).WithMessage("A maximum of 10 tags are allowed.");

        RuleForEach(x => x.Tags)
            .NotEmpty().WithMessage("Tags must not be empty.")
            .MaximumLength(50).WithMessage("Each tag must not exceed 50 characters.");
    }
}
