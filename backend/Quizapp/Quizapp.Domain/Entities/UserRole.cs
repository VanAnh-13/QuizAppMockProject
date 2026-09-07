namespace Quizapp.Domain.Entities;

public class UserRole
{
    public required Guid UserId { get; init; }
    public required Guid RoleId { get; init; }
    public User UserNavigation { get; init; } = null!;
    public Role RoleNavigation { get; init; } = null!;
}
