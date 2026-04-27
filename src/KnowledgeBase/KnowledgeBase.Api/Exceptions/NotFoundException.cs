namespace KnowledgeBase.Api.Exceptions;

public class NotFoundException(string message) : Exception(message)
{
    public static NotFoundException For<T>(Guid id) =>
        new($"{typeof(T).Name} '{id}' was not found.");
}
