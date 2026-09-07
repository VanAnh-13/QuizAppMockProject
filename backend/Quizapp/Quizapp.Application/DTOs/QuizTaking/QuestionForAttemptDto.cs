using Quizapp.Domain.Enums;

namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class QuestionForAttemptDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Image { get; set; }
    public QuestionLevel? Level { get; set; }
    public QuestionType QuestionType { get; set; }
    public int Order { get; set; }
    public IReadOnlyList<AnswerOptionDto> Answers { get; set; } = [];

    public sealed class Builder
    {
        private readonly QuestionForAttemptDto _dto = new();

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

        public Builder WithOrder(int order)
        {
            _dto.Order = order;
            return this;
        }

        public Builder WithAnswers(IEnumerable<AnswerOptionDto> answers)
        {
            ArgumentNullException.ThrowIfNull(answers);
            _dto.Answers = answers.ToList();
            return this;
        }

        public QuestionForAttemptDto Build()
        {
            var result = (QuestionForAttemptDto)_dto.MemberwiseClone();
            result.Answers = _dto.Answers.ToList();
            return result;
        }
    }
}
