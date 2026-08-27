using Microsoft.AspNetCore.Identity;

namespace QuizApp.Domain.Entities;

/// <summary>
/// Identity role extended with the description/active flag required by IRoleService.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ApplicationRole()
    {
    }

    public ApplicationRole(string name, string? description = null) : base(name)
    {
        Description = description;
    }
}
