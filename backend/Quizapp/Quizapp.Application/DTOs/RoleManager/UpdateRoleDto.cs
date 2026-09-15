namespace Quizapp.Application.DTOs.RoleManager;

public sealed class UpdateRoleDto
{
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
