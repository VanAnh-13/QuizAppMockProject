namespace QuizApp.Application.DTOs.QuizCodes;

// ---- Quiz-taking ViewModels (from ANG.P.L001.Opt1)

public class PrepareQuizViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string QuizId { get; set; } = string.Empty;
    public string QuizCode { get; set; } = string.Empty;
}

public class QuizPrepareInfoViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string QuizCode { get; set; } = string.Empty;

    /// <summary>Basic user info shown on the prepare screen.</summary>
    public QuizUserSnapshot User { get; set; } = new();
}

public class QuizUserSnapshot
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class TakeQuizViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string QuizId { get; set; } = string.Empty;
    public string QuizCode { get; set; } = string.Empty;
}

/// <summary>
/// Returned by takeQuiz. AnswerForTestViewModel deliberately omits isCorrect
/// so the browser never receives grading information during an active attempt.
/// </summary>
public class QuizForTestViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string QuizCode { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public IReadOnlyList<QuestionForTestViewModel> Questions { get; set; } = [];
}

public class QuestionForTestViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Domain.Enums.QuestionType QuestionType { get; set; }
    public IReadOnlyList<AnswerForTestViewModel> Answers { get; set; } = [];
}

/// <summary>isCorrect is intentionally absent — grading is server-only.</summary>
public class AnswerForTestViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

// ---- Submission

public class QuizSubmissionViewModel
{
    public string QuizId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string QuizCode { get; set; } = string.Empty;
    public IReadOnlyList<UserAnswerSubmissionViewModel> Answers { get; set; } = [];
}

/// <summary>
/// Extended beyond the handout's single answerId to support MultipleChoice and text answers,
/// while keeping answerId working for single-choice so the documented shape still validates.
/// </summary>
public class UserAnswerSubmissionViewModel
{
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>Single-choice, true/false.</summary>
    public string? AnswerId { get; set; }

    /// <summary>Multiple-choice (all-or-nothing grading).</summary>
    public IReadOnlyList<string>? AnswerIds { get; set; }

    /// <summary>Fill-in-the-blanks, short answer, long answer.</summary>
    public string? TextAnswer { get; set; }
}

public class QuizResultViewModel
{
    public string AttemptId { get; set; } = string.Empty;
    public string QuizTitle { get; set; } = string.Empty;
    public int Score { get; set; }
    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
    public string SubmittedAt { get; set; } = string.Empty;
}

// ---- Bulk code generation (bonus)

public class BulkCodeRequestViewModel
{
    public string QuizId { get; set; } = string.Empty;
    public IReadOnlyList<string> UserIds { get; set; } = [];
}

public class BulkCodeResultViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ExpiresAt { get; set; } = string.Empty;
}

// ---- Self-issue (Start button)

public class SelfIssueCodeViewModel
{
    public string QuizCode { get; set; } = string.Empty;
}
