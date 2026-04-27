namespace KnowledgeBase.Api.Services;

public interface ICurrentUserService
{
    string UserId { get; }
    string DisplayName { get; }
}
