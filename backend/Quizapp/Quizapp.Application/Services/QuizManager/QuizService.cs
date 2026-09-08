using FluentValidation;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Common;
using Quizapp.Application.DTOs.QuizManager.QuizQuestion;
using Quizapp.Application.DTOs.QuizManager.Quizzes;
using Quizapp.Application.Services.Common;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Application.Services.QuizManager;

public sealed class QuizService(
    IQuizRepository quizzes,
    IQuestionRepository questions,
    IUnitOfWork unitOfWork,
    ServiceAuthorization authorization,
    TimeProvider clock,
    IValidator<CreateQuizDto> createValidator,
    IValidator<UpdateQuizDto> updateValidator,
    IValidator<AddQuestionToQuizDto> addQuestionValidator) : IQuizService
{
    public async Task<QuizDto> GetByIdAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);

        return DtoMapping.ToDto(await FindAsync(quizId, cancellationToken));
    }

    public async Task<PagedResultDto<QuizDto>> GetListAsync(int pageNumber, int pageSize, string? search = null,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        ServiceRules.ValidatePage(pageNumber, pageSize);

        return ServiceRules.MapPage(
            await quizzes.GetListAsync(pageNumber, pageSize, ServiceRules.Search(search), cancellationToken),
            DtoMapping.ToDto);
    }

    public async Task<QuizDto> CreateAsync(CreateQuizDto request, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var now = clock.GetUtcNow()
            .UtcDateTime;

        var quiz = new Quiz
        {
            Id = Guid.NewGuid(), Title = request.Title.Trim(), Description = request.Description,
            Duration = request.Duration, Image = request.Image, PassedScore = request.PassedScore,
            IsActive = request.IsActive, CreateAt = now, UpdateAt = now
        };

        quizzes.Add(quiz);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DtoMapping.ToDto(quiz);
    }

    public async Task UpdateAsync(Guid quizId, UpdateQuizDto request, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var quiz = await FindAsync(quizId, cancellationToken);
        quiz.Title = request.Title.Trim();
        quiz.Description = request.Description;
        quiz.Duration = request.Duration;
        quiz.Image = request.Image;
        quiz.PassedScore = request.PassedScore;
        quiz.IsActive = request.IsActive;

        quiz.UpdateAt = clock.GetUtcNow()
            .UtcDateTime;

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(Guid quizId, bool isActive, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        var quiz = await FindAsync(quizId, cancellationToken);
        quiz.IsActive = isActive;

        quiz.UpdateAt = clock.GetUtcNow()
            .UtcDateTime;

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<QuizQuestionDto> AddQuestionAsync(AddQuestionToQuizDto request,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        await addQuestionValidator.ValidateAndThrowAsync(request, cancellationToken);
        var quiz = await FindAsync(request.QuizId, cancellationToken);

        var question = await questions.GetByIdAsync(request.QuestionId, cancellationToken)
                       ?? throw new NotFoundException(nameof(Question), request.QuestionId);

        if (quiz.QuizQuestions.Any(qq => qq.QuestionId == request.QuestionId))
            throw new ConflictException(nameof(QuizQuestion), nameof(QuizQuestion.QuestionId),
                request.QuestionId.ToString());

        var nextOrder = quiz.QuizQuestions.Count == 0
            ? QuizQuestion.FirstOrder
            : quiz.QuizQuestions.Max(qq => qq.Order) + 1;

        var assignment = new QuizQuestion
        {
            Id = Guid.NewGuid(), QuizId = quiz.Id, QuestionId = question.Id,
            QuizNavigation = quiz, QuestionNavigation = question, Order = nextOrder
        };

        quiz.QuizQuestions.Add(assignment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DtoMapping.ToDto(assignment);
    }

    public async Task RemoveQuestionAsync(Guid quizId, Guid questionId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        var quiz = await FindAsync(quizId, cancellationToken);

        var assignment = quiz.QuizQuestions.FirstOrDefault(qq => qq.QuestionId == questionId)
                         ?? throw new NotFoundException(nameof(QuizQuestion), questionId);

        quiz.QuizQuestions.Remove(assignment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReorderQuestionAsync(Guid quizId, IReadOnlyList<Guid> orderedQuestionIds,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        var quiz = await FindAsync(quizId, cancellationToken);

        var questionMap = quiz.QuizQuestions.ToDictionary(qq => qq.QuestionId);

        if (orderedQuestionIds.Count != questionMap.Count ||
            !orderedQuestionIds.ToHashSet()
                .SetEquals(questionMap.Keys))
            throw new Quizapp.Domain.Exceptions.ValidationException(nameof(orderedQuestionIds),
                "The provided question IDs must exactly match the quiz's assigned questions.");

        for (var i = 0; i < orderedQuestionIds.Count; i++)
            questionMap[orderedQuestionIds[i]].Order = QuizQuestion.FirstOrder + i;

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Quiz> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await quizzes.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Quiz), id);
}