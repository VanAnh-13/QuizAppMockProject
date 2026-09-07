namespace Quizapp.Application.DTOs.QuizHistory;

public class QuizAttemptDetailDto : QuizAttemptDto
{
    public IReadOnlyList<UserAnswerResultDto> Answers { get; set; } = [];

    public new sealed class Builder
    {
        private readonly QuizAttemptDetailDto _dto = new();

        public Builder WithId(Guid id)
        {
            _dto.Id = id;
            return this;
        }

        public Builder WithQuizId(Guid quizId)
        {
            _dto.QuizId = quizId;
            return this;
        }

        public Builder WithQuizTitle(string quizTitle)
        {
            ArgumentNullException.ThrowIfNull(quizTitle);
            _dto.QuizTitle = quizTitle;
            return this;
        }

        public Builder WithSubmittedAt(DateTime submittedAt)
        {
            _dto.SubmittedAt = submittedAt;
            return this;
        }

        public Builder WithScore(double score)
        {
            _dto.Score = score;
            return this;
        }

        public Builder WithAnswers(IEnumerable<UserAnswerResultDto> answers)
        {
            ArgumentNullException.ThrowIfNull(answers);
            _dto.Answers = [.. answers];
            return this;
        }

        public QuizAttemptDetailDto Build()
        {
            var result = (QuizAttemptDetailDto)_dto.MemberwiseClone();
            result.Answers = [.. _dto.Answers];
            return result;
        }
    }
}
