using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Common;
using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Application.Services.Common;
using Quizapp.Domain.Entities;

namespace Quizapp.Application.Services.QuizTaking;

public sealed class PublicQuizCatalogService(IQuizRepository quizzes) : IPublicQuizCatalogService
{
    public async Task<PagedResultDto<PublicQuizSummaryDto>> GetActiveQuizzesAsync(int pageNumber, int pageSize,
        string? search = null, CancellationToken cancellationToken = default)
    {
        ServiceRules.ValidatePage(pageNumber, pageSize);
        var page = await quizzes.GetActiveListAsync(pageNumber, pageSize, ServiceRules.Search(search),
            cancellationToken);

        return ServiceRules.MapPage(page, ToDto);
    }

    private static PublicQuizSummaryDto ToDto(Quiz quiz) => new()
    {
        Id = quiz.Id,
        Title = quiz.Title,
        Description = quiz.Description,
        Duration = quiz.Duration,
        PassedScore = quiz.PassedScore,
        QuestionCount = quiz.QuizQuestions.Count(qq => qq.QuestionNavigation.IsActive),
        UpdatedAt = quiz.UpdateAt
    };
}
