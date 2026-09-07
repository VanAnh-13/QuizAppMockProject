namespace Quizapp.Application.DTOs.QuizManager.Quizzes;

public class QuizDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Duration { get; set; }
    public string? Image { get; set; }
    public double? PassedScore { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public sealed class Builder
    {
        private readonly QuizDto _dto = new();

        public Builder WithId(Guid id)
        {
            _dto.Id = id;
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

        public Builder WithIsActive(bool isActive)
        {
            _dto.IsActive = isActive;
            return this;
        }

        public Builder WithCreatedAt(DateTime createdAt)
        {
            _dto.CreatedAt = createdAt;
            return this;
        }

        public Builder WithUpdatedAt(DateTime updatedAt)
        {
            _dto.UpdatedAt = updatedAt;
            return this;
        }

        public QuizDto Build()
        {
            return (QuizDto)_dto.MemberwiseClone();
        }
    }
}
