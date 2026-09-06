namespace Quizapp.Application.DTOs.UserManager;

public sealed class CreateUserDto
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string ConfirmPassword { get; init; }
    public required UserProfileDto Profile { get; init; }
    public bool IsActive { get; init; }
}
