namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class SubmitAnswerDto
{
    public Guid QuestionId { get; set; }
    public List<Guid> AnswerIds { get; set; } = [];
    public string? ResponseText { get; set; }

    public sealed class Builder
    {
        private readonly SubmitAnswerDto _dto = new();

        public Builder WithQuestionId(Guid questionId)
        {
            _dto.QuestionId = questionId;
            return this;
        }

        public Builder WithAnswerIds(IEnumerable<Guid> answerIds)
        {
            ArgumentNullException.ThrowIfNull(answerIds);
            _dto.AnswerIds = [.. answerIds];
            return this;
        }

        public Builder WithResponseText(string? responseText)
        {
            _dto.ResponseText = responseText;
            return this;
        }

        public SubmitAnswerDto Build()
        {
            var result = (SubmitAnswerDto)_dto.MemberwiseClone();
            result.AnswerIds = [.. _dto.AnswerIds];
            return result;
        }
    }
}
