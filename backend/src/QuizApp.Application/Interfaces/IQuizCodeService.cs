using QuizApp.Application.DTOs.QuizCodes;

namespace QuizApp.Application.Interfaces;

/// <summary>
/// Manages quiz access codes:
/// - Self-issue for the Start button path
/// - Validate an existing code (consumed by prepare/take)
/// - Bulk generate codes for an admin session
/// </summary>
public interface IQuizCodeService
{
    /// <summary>
    /// Issues (or returns an existing unused) code for the given (quiz, user) pair.
    /// Used by the Start button on a quiz card.
    /// </summary>
    Task<SelfIssueCodeViewModel> SelfIssueAsync(Guid quizId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Validates that the code exists, is not used, is not expired, and belongs to the caller.
    /// Throws the appropriate AppException on failure.
    /// </summary>
    Task ValidateAsync(string code, Guid callerUserId, CancellationToken ct = default);

    /// <summary>
    /// Bulk-generates one unused code per (quiz, user) pair for an admin session.
    /// Returns the generated codes for display and CSV export.
    /// </summary>
    Task<IReadOnlyList<BulkCodeResultViewModel>> BulkGenerateAsync(
        BulkCodeRequestViewModel request,
        CancellationToken ct = default);
}
