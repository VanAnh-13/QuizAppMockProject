using QuizApp.Application.Common.Paging;
using QuizApp.Application.DTOs.Questions;
using QuizApp.Application.DTOs.Quizzes;

namespace QuizApp.Application.Interfaces;

/// <summary>Backend mirror of the Angular IQuizService contract.</summary>
public interface IQuizService
{
    Task<PagedResult<QuizViewModel>> GetPagedAsync(PagedQuery query, bool activeOnly, CancellationToken ct = default);

    Task<IReadOnlyList<QuizViewModel>> GetActiveAsync(CancellationToken ct = default);

    Task<QuizViewModel> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<QuizViewModel> CreateAsync(QuizCreateViewModel model, CancellationToken ct = default);

    Task<QuizViewModel> UpdateAsync(QuizEditViewModel model, CancellationToken ct = default);

    /// <summary>Hard-deletes when no attempts exist; otherwise soft-deactivates to preserve history.</summary>
    Task<QuizDeleteResult> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<AssignedQuestionViewModel>> GetQuestionsByQuizIdAsync(Guid quizId, CancellationToken ct = default);

    Task AddQuestionToQuizAsync(QuizQuestionCreateViewModel model, CancellationToken ct = default);

    Task RemoveQuestionFromQuizAsync(Guid quizId, Guid quizQuestionId, CancellationToken ct = default);
}

public class QuizDeleteResult
{
    public bool HardDeleted { get; init; }
    public string Message { get; init; } = string.Empty;
}
