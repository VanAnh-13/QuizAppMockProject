using QuizApp.Application.DTOs.QuizCodes;
using QuizApp.Domain.Entities;
using QuizApp.Domain.Enums;

namespace QuizApp.Application.Interfaces;

/// <summary>
/// The only place that knows how each QuestionType is graded.
/// Pure logic with no I/O — easily unit-tested without a database.
/// </summary>
public interface IScoringService
{
    /// <summary>
    /// Grades a set of user answers against the correct answers from the question bank.
    /// Returns scored UserAnswer entities ready for persistence.
    /// </summary>
    ScoringResult Score(
        IReadOnlyList<QuestionForScoring> questions,
        IReadOnlyList<UserAnswerSubmissionViewModel> submission);
}

/// <summary>Everything needed to grade one question, assembled from the DB before calling ScoringService.</summary>
public class QuestionForScoring
{
    public Guid QuestionId { get; set; }
    public string QuestionContent { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public IReadOnlyList<AnswerForScoring> Answers { get; set; } = [];
}

public class AnswerForScoring
{
    public Guid AnswerId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class ScoringResult
{
    /// <summary>Partially-populated UserAnswer rows (without QuizAttemptId; caller fills that).</summary>
    public IReadOnlyList<UserAnswer> UserAnswers { get; init; } = [];
    public int CorrectCount { get; init; }

    /// <summary>LongAnswer questions are excluded from this count.</summary>
    public int GradableCount { get; init; }

    /// <summary>round(CorrectCount / GradableCount * 100), or 0 if GradableCount == 0.</summary>
    public int Score { get; init; }
}
