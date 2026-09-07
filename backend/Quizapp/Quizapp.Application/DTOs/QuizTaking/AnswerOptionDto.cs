namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class AnswerOptionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;

    public sealed class Builder
    {
        private readonly AnswerOptionDto _dto = new();

        public Builder WithId(Guid id)
        {
            _dto.Id = id;

            return this;
        }

        public Builder WithText(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            _dto.Text = text;

            return this;
        }

        public AnswerOptionDto Build()
        {
            return (AnswerOptionDto)_dto.MemberwiseClone();
        }
    }
}
