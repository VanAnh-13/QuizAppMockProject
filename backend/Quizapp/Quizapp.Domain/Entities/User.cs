using Quizapp.Domain.Enums;

namespace Quizapp.Domain.Entities;

public class User
{
    public Guid Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Avatar { get; init; }
    public UserStatus Status { get; init; }
    public bool IsActive
    {
        get => Status == UserStatus.Active;
        init => Status = value ? UserStatus.Active : UserStatus.Deactivated;
    }
    public DateTime CreateAt { get; init; }
    public DateTime UpdateAt { get; init; }

    public ICollection<QuizAttempt> QuizAttempts { get; init; } = new List<QuizAttempt>();
    public ICollection<Role> Roles { get; init; } = new List<Role>();
    public ICollection<UserRole> UserRoles { get; init; } = new List<UserRole>();
}
