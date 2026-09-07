namespace Quizapp.Domain.Entities;

public class Role
{
    public Guid Id { get; init; }
    public required string RoleName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public ICollection<User> Users { get; init; } = new List<User>();
    public ICollection<UserRole> UserRoles { get; init; } = new List<UserRole>();
}
