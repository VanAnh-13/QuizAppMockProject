using Quizapp.Application.DTOs.RoleManager;

namespace Quizapp.Application.DTOs.UserManager;

public sealed class UserDto
{
    public Guid Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public string? FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Avatar { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<RoleDto> Roles { get; init; } = [];
}
