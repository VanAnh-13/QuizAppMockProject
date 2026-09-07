namespace Quizapp.Domain.Exceptions;

public class ForbiddenException(string message = "You do not have permission to perform this action.")
    : QuizAppException(message, errorCode: "ACCESS_DENIED");
