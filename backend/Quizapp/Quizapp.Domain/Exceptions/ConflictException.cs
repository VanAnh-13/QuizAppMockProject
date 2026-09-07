namespace Quizapp.Domain.Exceptions;

public class ConflictException(string entityName, string conflictField, string value) : QuizAppException(
    $"{entityName} with {conflictField} '{value}' already exists.",
    errorCode: "ENTITY_CONFLICT")
{
    public string EntityName { get; } = entityName;
    public string ConflictField { get; } = conflictField;
}
