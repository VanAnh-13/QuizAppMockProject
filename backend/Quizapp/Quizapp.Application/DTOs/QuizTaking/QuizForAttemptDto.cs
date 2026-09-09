namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class QuizForAttemptDto
{
    public Guid QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Duration { get; set; }
    public string? Image { get; set; }
    public double? PassedScore { get; set; }
    public IReadOnlyList<QuestionForAttemptDto> Questions { get; set; } = [];

    public sealed class Builder
    {
        private readonly QuizForAttemptDto _dto = new();

        public Builder WithQuizId(Guid quizId)
        {
            _dto.QuizId = quizId;

            return this;
        }

        public Builder WithTitle(string title)
        {
            ArgumentNullException.ThrowIfNull(title);
            _dto.Title = title;

            return this;
        }

        public Builder WithDescription(string? description)
        {
            _dto.Description = description;

            return this;
        }

        public Builder WithDuration(int duration)
        {
            _dto.Duration = duration;

            return this;
        }

        public Builder WithImage(string? image)
        {
            _dto.Image = image;

            return this;
        }

        public Builder WithPassedScore(double? passedScore)
        {
            _dto.PassedScore = passedScore;

            return this;
        }

        public Builder WithQuestions(IEnumerable<QuestionForAttemptDto> questions)
        {
            ArgumentNullException.ThrowIfNull(questions);
            _dto.Questions = [.. questions];

            return this;
        }

        public QuizForAttemptDto Build()
        {
            var result = (QuizForAttemptDto)_dto.MemberwiseClone();
            result.Questions = [.. _dto.Questions];

            return result;
        }
    }
}
