using QuizApp.Domain.Enums;

namespace QuizApp.Domain.Entities;

/// <summary>
/// A user's answer for one question in one attempt. Content snapshots keep
/// attempt history reviewable even after questions/answers are deleted.
/// </summary>
public class UserAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuizAttemptId { get; set; }

    /// <summary>Nullable: SetNull on question deletion so history survives.</summary>
    public Guid? QuestionId { get; set; }

    /// <summary>Selected choice (single/multiple choice, true/false).</summary>
    public Guid? AnswerId { get; set; }

    /// <summary>Free text (fill-in-the-blanks, short/long answer).</summary>
    public string? TextAnswer { get; set; }

    public bool IsCorrect { get; set; }

    /// <summary>Long-answer questions are stored for manual review, not auto-graded.</summary>
    public bool NeedsReview { get; set; }

    // Snapshots taken at submission time
    public string QuestionContent { get; set; } = string.Empty;

    public QuestionType QuestionType { get; set; }

    /// <summary>The user's answer(s) rendered as text.</summary>
    public string? AnswerContent { get; set; }

    /// <summary>The correct answer(s) rendered as text (never sent during an attempt).</summary>
    public string? CorrectAnswerContent { get; set; }

    // Navigation properties
    public QuizAttempt QuizAttempt { get; set; } = null!;

    public Question? Question { get; set; }

    public Answer? Answer { get; set; }
}
