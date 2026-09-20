namespace Quizapp.Domain.Exceptions;

public class QuizAppException : Exception
{
    public string? ErrorCode { get; }

    public QuizAppException(string message, string? errorCode = null)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public QuizAppException(string message, Exception innerException, string? errorCode = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
