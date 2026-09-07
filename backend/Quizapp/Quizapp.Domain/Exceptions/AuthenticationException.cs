namespace Quizapp.Domain.Exceptions;

public class AuthenticationException(string message = "Authentication failed.")
    : QuizAppException(message, errorCode: "AUTHENTICATION_FAILED");
