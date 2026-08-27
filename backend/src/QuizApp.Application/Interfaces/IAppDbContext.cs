using Microsoft.EntityFrameworkCore;
using QuizApp.Domain.Entities;

namespace QuizApp.Application.Interfaces;

/// <summary>
/// Data access abstraction implemented by AppDbContext (Infrastructure).
/// Lets Application-layer services stay provider-agnostic.
/// </summary>
public interface IAppDbContext
{
    DbSet<Quiz> Quizzes { get; }
    DbSet<Question> Questions { get; }
    DbSet<Answer> Answers { get; }
    DbSet<QuizQuestion> QuizQuestions { get; }
    DbSet<QuizCode> QuizCodes { get; }
    DbSet<QuizAttempt> QuizAttempts { get; }
    DbSet<UserAnswer> UserAnswers { get; }
    DbSet<Feedback> Feedbacks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
