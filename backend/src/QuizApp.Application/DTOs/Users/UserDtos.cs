namespace QuizApp.Application.DTOs.Users;

/// <summary>Admin user creation (no password editing on edit - ChangePassword is a separate flow).</summary>
public class UserCreateViewModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class UserEditViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public bool IsActive { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
}

public class UpdateUserRolesViewModel
{
    public string[] Roles { get; set; } = Array.Empty<string>();
}

public class UpdateUserStatusViewModel
{
    public bool IsActive { get; set; }
}
