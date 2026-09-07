using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quizapp.Application.Strategies.QuizManager.Questions;
using Quizapp.Application.Validators.QuizManager.Quizzes;

namespace Quizapp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateQuizDtoValidator>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IQuestionCreationStrategy, SingleChoiceQuestionStrategy>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IQuestionCreationStrategy, MultipleChoiceQuestionStrategy>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IQuestionCreationStrategy, TrueFalseQuestionStrategy>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IQuestionCreationStrategy, TextQuestionStrategy>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IQuestionCreationStrategy, LongAnswerQuestionStrategy>());

        return services;
    }
}
