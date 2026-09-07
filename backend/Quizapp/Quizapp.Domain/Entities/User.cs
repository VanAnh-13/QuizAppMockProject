using Quizapp.Domain.Enums;

namespace Quizapp.Domain.Entities;

public class User
{
    public Guid Id { get; init; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Avatar { get; set; }
    public UserStatus Status { get; set; }

    public bool IsActive
    {
        get => Status == UserStatus.Active;
        set => Status = value ? UserStatus.Active : UserStatus.Deactivated;
    }

    public Guid SecurityStamp { get; set; } = Guid.NewGuid();
    public DateTime CreateAt { get; init; }
    public DateTime UpdateAt { get; set; }
    public ICollection<QuizAttempt> QuizAttempts { get; init; } = new List<QuizAttempt>();
    public ICollection<Role> Roles { get; init; } = new List<Role>();
    public ICollection<UserRole> UserRoles { get; init; } = new List<UserRole>();
}
