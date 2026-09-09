namespace Quizapp.Application.DTOs.QuizManager.Answers;

public class AnswerDto
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsActive { get; set; }

    public sealed class Builder
    {
        private readonly AnswerDto _dto = new();

        public Builder WithId(Guid id)
        {
            _dto.Id = id;

            return this;
        }

        public Builder WithQuestionId(Guid questionId)
        {
            _dto.QuestionId = questionId;

            return this;
        }

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

        public AnswerDto Build()
        {
            return (AnswerDto)_dto.MemberwiseClone();
        }
    }
}
