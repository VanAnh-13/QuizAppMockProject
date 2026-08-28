using QuizApp.Application.DTOs.Attempts;
using QuizApp.Application.DTOs.QuizCodes;

namespace QuizApp.Application.Interfaces;

/// <summary>
/// Manages the attempt lifecycle: Prepared → InProgress → Submitted | Expired.
/// All timing and scoring is server-side.
/// </summary>
public interface IQuizAttemptService
{
    /// <summary>
    /// Validates the quiz code and creates a new attempt in the Prepared state.
    /// Returns the quiz info shown on the prepare screen.
    /// </summary>
    Task<QuizPrepareInfoViewModel> PrepareAsync(PrepareQuizViewModel model, Guid callerUserId, CancellationToken ct = default);

    /// <summary>
    /// Transitions Prepared → InProgress, records StartTime server-side, computes Deadline.
    /// Returns the quiz with questions and answers (no isCorrect).
    /// </summary>
    Task<QuizForTestViewModel> TakeAsync(TakeQuizViewModel model, Guid callerUserId, CancellationToken ct = default);

    /// <summary>
    /// Grades the submission, persists UserAnswer rows, transitions → Submitted (or Expired).
    /// Repeat submission returns 409.
    /// </summary>
    Task<QuizResultViewModel> SubmitAsync(QuizSubmissionViewModel model, Guid callerUserId, CancellationToken ct = default);

    /// <summary>Returns all attempts for the authenticated user, newest first.</summary>
    Task<IReadOnlyList<AttemptSummaryViewModel>> GetMyAttemptsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the full detail of an attempt.
    /// Only the attempt owner or a manager may call this.
    /// </summary>
    Task<AttemptDetailViewModel> GetAttemptDetailAsync(Guid attemptId, Guid callerUserId, bool isManager, CancellationToken ct = default);
}
