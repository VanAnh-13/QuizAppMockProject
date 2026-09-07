namespace Quizapp.Application.DTOs.QuizManager.Quizzes;

public class CreateQuizDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Duration { get; set; }
    public string? Image { get; set; }
    public double PassedScore { get; set; }
    public bool IsActive { get; set; }

    public sealed class Builder
    {
        private readonly CreateQuizDto _dto = new();

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

        public Builder WithPassedScore(double passedScore)
        {
            _dto.PassedScore = passedScore;
            return this;
        }

        public Builder WithIsActive(bool isActive)
        {
            _dto.IsActive = isActive;
            return this;
        }

        public CreateQuizDto Build()
        {
            return (CreateQuizDto)_dto.MemberwiseClone();
        }
    }
}
