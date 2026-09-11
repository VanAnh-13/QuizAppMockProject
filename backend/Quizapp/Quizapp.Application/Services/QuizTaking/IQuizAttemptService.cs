using Quizapp.Application.DTOs.Common;
using Quizapp.Application.DTOs.QuizHistory;
using Quizapp.Application.DTOs.QuizTaking;

namespace Quizapp.Application.Services.QuizTaking;

public interface IQuizAttemptService
{
    Task<QuizAttemptDetailDto> SubmitSavedAsync(Guid attemptId, AttemptRevisionDto request,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<QuizAttemptSummaryDto>> GetInProgressAsync(int pageNumber, int pageSize,
        Guid? quizId = null, CancellationToken cancellationToken = default);

    Task<QuizAttemptProgressDto> PauseAsync(Guid attemptId, SaveQuizProgressDto request,
        CancellationToken cancellationToken = default);

    Task<QuizAttemptProgressDto> ResumeAsync(Guid attemptId, AttemptRevisionDto request,
        CancellationToken cancellationToken = default);

    Task<QuizAttemptProgressDto> GetProgressAsync(Guid attemptId, CancellationToken cancellationToken = default);

    Task<QuizAttemptProgressDto> SaveProgressAsync(Guid attemptId, SaveQuizProgressDto request,
        CancellationToken cancellationToken = default);

    Task<QuizAttemptStartDto> StartAsync(
        Guid quizId,
        CancellationToken cancellationToken = default);

    Task<QuizAttemptDetailDto> SubmitAsync(
        Guid quizId,
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
