namespace QuizApp.Application.DTOs.Users;

/// <summary>Matches UserViewModel from ANG.P.L001.Opt1.</summary>
public class UserViewModel
{
    public string Id { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>ISO date (yyyy-MM-dd).</summary>
    public string DateOfBirth { get; set; } = string.Empty;

    public string Avatar { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string[] Roles { get; set; } = Array.Empty<string>();
}
