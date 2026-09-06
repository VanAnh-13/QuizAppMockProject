namespace Quizapp.Domain.Entities;

public class UserAnswer
{
    public Guid Id { get; init; }
    public required Guid QuizAttemptId { get; init; }
    public required Guid QuestionId { get; init; }
    public Guid? AnswerId { get; init; }
    public string? ResponseText { get; init; }

    public UserAnswer()
    {
    }

    public UserAnswer(Guid id, Guid quizAttemptId, Guid questionId, Guid answerId)
    {
        Id = id;
        QuizAttemptId = quizAttemptId;
        QuestionId = questionId;
        AnswerId = answerId;
    }

    public QuizAttempt QuizAttemptNavigation { get; init; } = null!;
    public Question QuestionNavigation { get; init; } = null!;
    public Answer? AnswerNavigation { get; init; }
}
