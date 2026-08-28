using QuizApp.Domain.Enums;

namespace QuizApp.Application.DTOs.Attempts;

/// <summary>One row in the attempt history list (GET /api/attempts/me).</summary>
public class AttemptSummaryViewModel
{
    public string Id { get; set; } = string.Empty;
    public string QuizId { get; set; } = string.Empty;
    public string QuizTitle { get; set; } = string.Empty;
    public int? Score { get; set; }
    public int? CorrectCount { get; set; }
    public int? TotalQuestions { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SubmittedAt { get; set; }
    public string? StartTime { get; set; }
}

/// <summary>Full attempt detail with per-question breakdown (GET /api/attempts/{id}).</summary>
public class AttemptDetailViewModel
{
    public string Id { get; set; } = string.Empty;
    public string QuizId { get; set; } = string.Empty;
    public string QuizTitle { get; set; } = string.Empty;
    public int? Score { get; set; }
    public int? CorrectCount { get; set; }
    public int? TotalQuestions { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SubmittedAt { get; set; }
    public string? StartTime { get; set; }
    public IReadOnlyList<AttemptAnswerViewModel> Answers { get; set; } = [];
}

/// <summary>Per-question breakdown shown in the attempt detail and history drill-down.</summary>
public class AttemptAnswerViewModel
{
    public string Id { get; set; } = string.Empty;
    public string QuestionContent { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public string? AnswerContent { get; set; }
    public string? CorrectAnswerContent { get; set; }
    public bool IsCorrect { get; set; }
    public bool NeedsReview { get; set; }
}
