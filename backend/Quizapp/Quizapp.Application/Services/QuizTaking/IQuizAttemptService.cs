using Quizapp.Application.DTOs.Common;
using Quizapp.Application.DTOs.QuizHistory;
using Quizapp.Application.DTOs.QuizTaking;

namespace Quizapp.Application.Services.QuizTaking;

public interface IQuizAttemptService
{
    Task<QuizAttemptStartDto> StartAsync(
        Guid quizId,
        CancellationToken cancellationToken = default);

    Task<QuizAttemptDetailDto> SubmitAsync(
        Guid attemptId,
        SubmitQuizDto request,
        CancellationToken cancellationToken = default);

    Task<QuizAttemptDetailDto> GetResultAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<QuizAttemptDto>> GetHistoryAsync(
        int pageNumber,
        int pageSize,
        Guid? quizId = null,
        CancellationToken cancellationToken = default);
}