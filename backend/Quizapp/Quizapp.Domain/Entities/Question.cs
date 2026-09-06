using Quizapp.Domain.Enums;

namespace Quizapp.Domain.Entities;

public class Question
{
    public Guid Id { get; init; }
    public required string Content { get; init; }
    public string? Image { get; init; }
    public QuestionLevel? Level { get; init; }
    public QuestionType QuestionType { get; init; }
    public bool IsActive { get; init; }

    public Question()
    {
    }

    public Question(Guid id, string content, QuestionType questionType)
    {
        Id = id;
        Content = content;
        QuestionType = questionType;
    }

    public ICollection<Quiz> Quizzes { get; init; } = new List<Quiz>();
    public ICollection<QuizQuestion> QuizQuestions { get; init; } = new List<QuizQuestion>();
    public ICollection<UserAnswer> UserAnswers { get; init; } = new List<UserAnswer>();
    public ICollection<Answer> Answers { get; init; } = new List<Answer>();
}
