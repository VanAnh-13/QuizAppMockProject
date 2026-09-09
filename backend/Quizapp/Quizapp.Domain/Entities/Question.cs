using Quizapp.Domain.Enums;

namespace Quizapp.Domain.Entities;

public class Question
{
    public Guid Id { get; init; }
    public required string Content { get; set; }
    public string? Image { get; set; }
    public QuestionLevel? Level { get; set; }
    public QuestionType QuestionType { get; set; }
    public bool IsActive { get; set; }
    public ICollection<Quiz> Quizzes { get; init; } = new List<Quiz>();
    public ICollection<QuizQuestion> QuizQuestions { get; init; } = new List<QuizQuestion>();
    public ICollection<UserAnswer> UserAnswers { get; init; } = new List<UserAnswer>();
    public ICollection<Answer> Answers { get; init; } = new List<Answer>();
}
