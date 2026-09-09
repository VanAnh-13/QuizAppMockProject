namespace Quizapp.Application.DTOs.QuizManager.Questions;

public sealed class CreateQuestionAnswerDto
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsActive { get; set; } = true;

    public sealed class Builder
    {
        private readonly CreateQuestionAnswerDto _dto = new();

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

        public CreateQuestionAnswerDto Build()
        {
            return (CreateQuestionAnswerDto)_dto.MemberwiseClone();
        }
    }
}
