using Quizapp.Domain.Enums;

namespace Quizapp.Application.DTOs.QuizManager.Questions;

public class QuestionDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Image { get; set; }
    public QuestionLevel? Level { get; set; }
    public QuestionType QuestionType { get; set; }
    public bool IsActive { get; set; }

    public sealed class Builder
    {
        private readonly QuestionDto _dto = new();

        public Builder WithId(Guid id)
        {
            _dto.Id = id;
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

        public Builder WithIsActive(bool isActive)
        {
            _dto.IsActive = isActive;
            return this;
        }

        public QuestionDto Build()
        {
            return (QuestionDto)_dto.MemberwiseClone();
        }
    }
}
