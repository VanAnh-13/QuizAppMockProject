using QuizQuestionEntity = Quizapp.Domain.Entities.QuizQuestion;

namespace Quizapp.Application.DTOs.QuizManager.QuizQuestion;

public class AddQuestionToQuizDto
{
    public Guid QuizId { get; set; }
    public Guid QuestionId { get; set; }
    public int Order { get; set; } = QuizQuestionEntity.FirstOrder;

    public sealed class Builder
    {
        private readonly AddQuestionToQuizDto _dto = new();

        public Builder WithQuizId(Guid quizId)
        {
            _dto.QuizId = quizId;

            return this;
        }

        public Builder WithQuestionId(Guid questionId)
        {
            _dto.QuestionId = questionId;

            return this;
        }

        public Builder WithOrder(int order)
        {
            _dto.Order = order;

            return this;
        }

        public AddQuestionToQuizDto Build()
        {
            return (AddQuestionToQuizDto)_dto.MemberwiseClone();
        }
    }
}
