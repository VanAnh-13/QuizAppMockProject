namespace QuizApp.Domain.Enums;

/// <summary>
/// Attempt state machine: Prepared → InProgress → Submitted | Expired.
/// </summary>
public enum QuizAttemptStatus
{
    Prepared = 0,
    InProgress = 1,
    Submitted = 2,
    Expired = 3
}
