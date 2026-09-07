namespace Quizapp.Domain.Entities;

public class Role
{
    public Guid Id { get; init; }
    public required string RoleName { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }

    public ICollection<User> Users { get; init; } = new List<User>();
    public ICollection<UserRole> UserRoles { get; init; } = new List<UserRole>();
}
