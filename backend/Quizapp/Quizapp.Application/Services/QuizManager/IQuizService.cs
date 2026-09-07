using Quizapp.Application.DTOs.Common;
using Quizapp.Application.DTOs.QuizManager.QuizQuestion;
using Quizapp.Application.DTOs.QuizManager.Quizzes;

namespace Quizapp.Application.Services.QuizManager;

public interface IQuizService
{
    Task<QuizDto> GetByIdAsync(
        Guid quizId,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<QuizDto>> GetListAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<QuizDto> CreateAsync(
        CreateQuizDto request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid quizId,
        UpdateQuizDto request,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        Guid quizId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<QuizQuestionDto> AddQuestionAsync(
        AddQuestionToQuizDto request,
        CancellationToken cancellationToken = default);

    Task RemoveQuestionAsync(
        Guid quizId,
        Guid questionId,
        CancellationToken cancellationToken = default);

    Task ReorderQuestionAsync(
        Guid quizId,
        IReadOnlyList<Guid> orderedQuestionIds,
        CancellationToken cancellationToken = default);
}