namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class SubmitQuizDto
{
    public Guid AttemptId { get; set; }
    public List<SubmitAnswerDto> Answers { get; set; } = [];

    public sealed class Builder
    {
        private readonly SubmitQuizDto _dto = new();

        public Builder WithAttemptId(Guid attemptId)
        {
            _dto.AttemptId = attemptId;
            return this;
        }

        public Builder WithAnswers(IEnumerable<SubmitAnswerDto> answers)
        {
            ArgumentNullException.ThrowIfNull(answers);
            _dto.Answers = [.. answers];

            return this;
        }

        public SubmitQuizDto Build()
        {
            var result = (SubmitQuizDto)_dto.MemberwiseClone();
            result.Answers = [.. _dto.Answers];

            return result;
        }
    }
}
