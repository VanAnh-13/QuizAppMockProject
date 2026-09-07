namespace Quizapp.Domain.Exceptions;

public class NotFoundException(string entityName, object entityId) : QuizAppException(
    $"{entityName} with ID '{entityId}' was not found.",
    errorCode: "ENTITY_NOT_FOUND")
{
    public string EntityName { get; } = entityName;
    public object EntityId { get; } = entityId;
}
