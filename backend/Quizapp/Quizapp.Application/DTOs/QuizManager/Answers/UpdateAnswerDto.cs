namespace Quizapp.Application.DTOs.QuizManager.Answers;

public class UpdateAnswerDto
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsActive { get; set; }

    public sealed class Builder
    {
        private readonly UpdateAnswerDto _dto = new();

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

        public UpdateAnswerDto Build()
        {
            return (UpdateAnswerDto)_dto.MemberwiseClone();
        }
    }
}
