using FluentValidation;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Common;
using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Application.Factories.QuizManager.Questions;
using Quizapp.Application.Services.Common;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Application.Services.QuestionManager;

public sealed class QuestionService(
    IQuestionRepository questions,
    IUnitOfWork unitOfWork,
    IQuestionFactory factory,
    ServiceAuthorization authorization,
    IValidator<UpdateQuestionDto> updateValidator) : IQuestionService
{
    public async Task<QuestionDto> GetByIdAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);

        return DtoMapping.ToDto(await FindAsync(questionId, cancellationToken));
    }

    public async Task<PagedResultDto<QuestionDto>> GetListAsync(int pageNumber, int pageSize, string? search = null,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        ServiceRules.ValidatePage(pageNumber, pageSize);

        return ServiceRules.MapPage(
            await questions.GetListAsync(pageNumber, pageSize, ServiceRules.Search(search), cancellationToken),
            DtoMapping.ToDto);
    }

    public async Task<QuestionDto> CreateAsync(CreateQuestionDto request, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        var question = factory.Create(request);
        questions.Add(question);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DtoMapping.ToDto(question);
    }

    public async Task UpdateAsync(Guid questionId, UpdateQuestionDto request,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var question = await FindAsync(questionId, cancellationToken);
        question.Content = request.Content;
        question.Image = request.Image;
        question.Level = request.Level;
        question.QuestionType = request.QuestionType;
        question.IsActive = request.IsActive;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(Guid questionId, bool isActive, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        var question = await FindAsync(questionId, cancellationToken);
        question.IsActive = isActive;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Question> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await questions.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Question), id);
}
