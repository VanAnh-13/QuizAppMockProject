namespace Quizapp.Domain.Exceptions;

public class BusinessRuleException(string ruleName, string message)
    : QuizAppException(message, errorCode: "BUSINESS_RULE_VIOLATION")
{
    public string RuleName { get; } = ruleName;
}
