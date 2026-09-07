namespace Quizapp.Application.DTOs.QuizHistory;

public class QuizAttemptDto
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public double Score { get; set; }

    public sealed class Builder
    {
        private readonly QuizAttemptDto _dto = new();

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

        public QuizAttemptDto Build()
        {
            return (QuizAttemptDto)_dto.MemberwiseClone();
        }
    }
}
