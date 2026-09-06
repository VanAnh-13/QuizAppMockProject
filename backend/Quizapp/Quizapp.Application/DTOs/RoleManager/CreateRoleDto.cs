namespace Quizapp.Application.DTOs.RoleManager;

public sealed class CreateRoleDto
{
    public required string RoleName { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}
