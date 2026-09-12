using Quizapp.Application.DTOs.Common;
using Quizapp.Application.DTOs.QuizTaking;

namespace Quizapp.Application.Services.QuizTaking;

public interface IPublicQuizCatalogService
{
    Task<PagedResultDto<PublicQuizSummaryDto>> GetActiveQuizzesAsync(int pageNumber, int pageSize,
        string? search = null, CancellationToken cancellationToken = default);
}
