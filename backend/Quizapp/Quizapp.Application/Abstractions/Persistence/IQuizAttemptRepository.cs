using Quizapp.Application.DTOs.Common;
using Quizapp.Domain.Entities;

namespace Quizapp.Application.Abstractions.Persistence;

public interface IQuizAttemptRepository
{
    Task<QuizAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResultDto<QuizAttempt>> GetHistoryAsync(Guid userId, int pageNumber, int pageSize, Guid? quizId, CancellationToken cancellationToken);
    void Add(QuizAttempt attempt);
}
