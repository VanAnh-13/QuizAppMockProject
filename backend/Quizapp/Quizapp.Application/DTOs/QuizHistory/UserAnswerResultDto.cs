using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Domain.Enums;

namespace Quizapp.Application.DTOs.QuizHistory;

public sealed class UserAnswerResultDto
{
    public Guid QuestionId { get; set; }
    public string QuestionContent { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public string? Image { get; set; }
    public QuestionLevel? Level { get; set; }
    public IReadOnlyList<AnswerOptionDto> SelectedAnswers { get; set; } = [];
    public string? ResponseText { get; set; }

    public sealed class Builder
    {
        private readonly UserAnswerResultDto _dto = new();

        public Builder WithQuestionId(Guid questionId)
        {
            _dto.QuestionId = questionId;
            return this;
        }

        public Builder WithQuestionContent(string questionContent)
        {
            ArgumentNullException.ThrowIfNull(questionContent);
            _dto.QuestionContent = questionContent;
            return this;
        }

        public Builder WithQuestionType(QuestionType questionType)
        {
            _dto.QuestionType = questionType;
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

        public Builder WithSelectedAnswers(IEnumerable<AnswerOptionDto> selectedAnswers)
        {
            ArgumentNullException.ThrowIfNull(selectedAnswers);
            _dto.SelectedAnswers = selectedAnswers.ToList();
            return this;
        }

        public Builder WithResponseText(string? responseText)
        {
            _dto.ResponseText = responseText;
            return this;
        }

        public UserAnswerResultDto Build()
        {
            var result = (UserAnswerResultDto)_dto.MemberwiseClone();
            result.SelectedAnswers = _dto.SelectedAnswers.ToList();
            return result;
        }
    }
}
