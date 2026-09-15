using QuizQuestionEntity = Quizapp.Domain.Entities.QuizQuestion;

namespace Quizapp.Application.DTOs.QuizManager.QuizQuestion;

public class AddQuestionToQuizDto
{
    public Guid QuizId { get; set; }
    public Guid QuestionId { get; set; }
    public int Order { get; set; } = QuizQuestionEntity.FirstOrder;
}
