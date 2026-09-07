using Quizapp.Application.DTOs.RoleManager;
using Quizapp.Domain.Entities;

namespace Quizapp.Application.Factories.RoleManager;

public interface IRoleFactory
{
    Role Create(CreateRoleDto request);
}
