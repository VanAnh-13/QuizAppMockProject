namespace Quizapp.Application.DTOs.UserManager;

public sealed class UpdateUserRolesDto
{
    public List<Guid> RoleIds { get; set; } = [];
}
