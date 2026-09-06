using System.Diagnostics.CodeAnalysis;
using QuizQuestionEntity = Quizapp.Domain.Entities.QuizQuestion;

namespace Quizapp.Application.DTOs.QuizManager.QuizQuestion;

public class AddQuestionToQuizDto
{
    public required Guid QuizId { get; init; }
    public required Guid QuestionId { get; init; }
    public int Order { get; init; } = QuizQuestionEntity.FirstOrder;

    public AddQuestionToQuizDto()
    {
    }

    [SetsRequiredMembers]
    public AddQuestionToQuizDto(Guid quizId, Guid questionId, int order)
    {
        QuizId = quizId;
        QuestionId = questionId;
        Order = order;
    }
}
