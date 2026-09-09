using Quizapp.Application.DTOs.Common;
using Quizapp.Application.DTOs.QuizManager.Questions;

namespace Quizapp.Application.Services.QuestionManager;

public interface IQuestionService
{
    Task<QuestionDto> GetByIdAsync(
        Guid questionId,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<QuestionDto>> GetListAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<QuestionDto> CreateAsync(
        CreateQuestionDto request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid questionId,
        UpdateQuestionDto request,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        Guid questionId,
        bool isActive,
        CancellationToken cancellationToken = default);
}