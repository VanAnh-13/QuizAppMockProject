namespace Quizapp.Domain.Exceptions;

public class ValidationException(IDictionary<string, string[]> errors) : QuizAppException(
    "One or more validation errors occurred.",
    errorCode: "VALIDATION_FAILED")
{
    public IDictionary<string, string[]> Errors { get; } = errors;

    public ValidationException(string field, string error)
        : this(new Dictionary<string, string[]>
        {
            { field, [error] }
        })
    {
    }
}
