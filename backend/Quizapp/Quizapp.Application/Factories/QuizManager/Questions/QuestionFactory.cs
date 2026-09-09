using FluentValidation;
using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Application.Strategies.QuizManager.Questions;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;

namespace Quizapp.Application.Factories.QuizManager.Questions;

public sealed class QuestionFactory : IQuestionFactory
{
    private readonly IValidator<CreateQuestionDto> _validator;
    private readonly Dictionary<QuestionType, IQuestionCreationStrategy> _strategies = [];

    public QuestionFactory(IValidator<CreateQuestionDto> validator, IEnumerable<IQuestionCreationStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(strategies);
        _validator = validator;

        foreach (var strategy in strategies)
        {
            foreach (var questionType in strategy.SupportedTypes)
            {
                if (!Enum.IsDefined(questionType))
                    throw new InvalidOperationException(
                        $"A creation strategy declares an unknown question type: {questionType}.");

                if (!_strategies.TryAdd(questionType, strategy))
                    throw new InvalidOperationException(
                        $"More than one creation strategy is registered for {questionType}.");
            }
        }
    }

    public Question Create(CreateQuestionDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _validator.ValidateAndThrow(request);

        if (!_strategies.TryGetValue(request.QuestionType, out var strategy))
            throw new NotSupportedException($"No creation strategy is registered for {request.QuestionType}.");

        strategy.ValidateAnswers([.. request.Answers.Where(answer => answer.IsActive)]);

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
        {
            question.Answers.Add(new Answer
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                QuestionNavigation = question,
                Text = answer.Text,
                IsCorrect = answer.IsCorrect,
                IsActive = answer.IsActive
            });
        }

        return question;
    }
}
