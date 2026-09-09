namespace Quizapp.Application.DTOs.QuizManager.Answers;

public class CreateAnswerDto
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsActive { get; set; }
    public Guid QuestionId { get; set; }

    public sealed class Builder
    {
        private readonly CreateAnswerDto _dto = new();

        public Builder WithText(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            _dto.Text = text;

            return this;
        }

        public Builder WithIsCorrect(bool isCorrect)
        {
            _dto.IsCorrect = isCorrect;

            return this;
        }

        public Builder WithIsActive(bool isActive)
        {
            _dto.IsActive = isActive;

            return this;
        }

        public Builder WithQuestionId(Guid questionId)
        {
            _dto.QuestionId = questionId;

            return this;
        }

        public CreateAnswerDto Build()
        {
            return (CreateAnswerDto)_dto.MemberwiseClone();
        }
    }
}
