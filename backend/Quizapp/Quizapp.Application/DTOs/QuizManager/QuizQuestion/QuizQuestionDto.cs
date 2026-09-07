using Quizapp.Domain.Enums;

namespace Quizapp.Application.DTOs.QuizManager.QuizQuestion;

public class QuizQuestionDto
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public Guid QuestionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Image { get; set; }
    public QuestionLevel? Level { get; set; }
    public QuestionType QuestionType { get; set; }
    public int Order { get; set; }

    public sealed class Builder
    {
        private readonly QuizQuestionDto _dto = new();

        public Builder WithId(Guid id)
        {
            _dto.Id = id;

            return this;
        }

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

        public Builder WithContent(string content)
        {
            ArgumentNullException.ThrowIfNull(content);
            _dto.Content = content;

            return this;
        }

        public Builder WithImage(string? image)
        {
            _dto.Image = image;

            return this;
        }

        public Builder WithLevel(QuestionLevel? level)
        {
            _dto.Level = level;

            return this;
        }

        public Builder WithQuestionType(QuestionType questionType)
        {
            _dto.QuestionType = questionType;

            return this;
        }

        public Builder WithOrder(int order)
        {
            _dto.Order = order;

            return this;
        }

        public QuizQuestionDto Build()
        {
            return (QuizQuestionDto)_dto.MemberwiseClone();
        }
    }
}
