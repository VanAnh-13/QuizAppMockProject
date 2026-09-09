using Quizapp.Domain.Enums;

namespace Quizapp.Application.DTOs.QuizManager.Questions;

public class CreateQuestionDto
{
    public string Content { get; set; } = string.Empty;
    public string? Image { get; set; }
    public QuestionLevel Level { get; set; }
    public QuestionType QuestionType { get; set; }
    public bool IsActive { get; set; }
    public List<CreateQuestionAnswerDto> Answers { get; set; } = [];

    public sealed class Builder
    {
        private readonly CreateQuestionDto _dto = new();

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

        public Builder WithLevel(QuestionLevel level)
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

        public Builder WithAnswers(IEnumerable<CreateQuestionAnswerDto> answers)
        {
            ArgumentNullException.ThrowIfNull(answers);
            _dto.Answers = [.. answers];

            return this;
        }

        public CreateQuestionDto Build()
        {
            var result = (CreateQuestionDto)_dto.MemberwiseClone();
            result.Answers = [.. _dto.Answers];

            return result;
        }
    }
}
