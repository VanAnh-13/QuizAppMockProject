using FluentValidation;
using FluentValidation.Results;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Common;
using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Application.Services.Common;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Application.Services.QuestionManager;

public sealed class QuestionService(
    IQuestionRepository questions,
    IUnitOfWork unitOfWork,
    IValidator<CreateQuestionDto> createValidator,
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
        var question = Create(request, createValidator);
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

    internal static Question Create(CreateQuestionDto request, IValidator<CreateQuestionDto> validator)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(validator);
        validator.ValidateAndThrow(request);
        var active = request.Answers.Where(answer => answer.IsActive).ToArray();
        var correct = active.Count(answer => answer.IsCorrect);
        Require(request.QuestionType switch
        {
            QuestionType.SingleChoice => active.Length >= 2 && correct == 1,
            QuestionType.MultipleChoice => active.Length >= 2 && correct >= 1,
            QuestionType.TrueFalse => active.Length == 2 && correct == 1,
            QuestionType.FillInTheBlanks or QuestionType.ShortAnswer => active.Length >= 1 && correct == active.Length,
            QuestionType.LongAnswer => correct == active.Length,
            _ => throw new NotSupportedException($"No creation rules are registered for {request.QuestionType}.")
        }, "The active answers do not match this question type.");

        var question = new Question
        {
            Id = Guid.NewGuid(),
            Content = request.Content,
            Image = request.Image,
            Level = request.Level,
            QuestionType = request.QuestionType,
            IsActive = request.IsActive
        };
        foreach (var answer in request.Answers)
            question.Answers.Add(new Answer
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                QuestionNavigation = question,
                Text = answer.Text,
                IsCorrect = answer.IsCorrect,
                IsActive = answer.IsActive
            });

        return question;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new FluentValidation.ValidationException([new ValidationFailure(nameof(CreateQuestionDto.Answers), message)]);
    }
}
