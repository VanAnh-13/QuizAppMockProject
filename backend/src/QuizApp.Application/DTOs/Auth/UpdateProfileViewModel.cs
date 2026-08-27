namespace QuizApp.Application.DTOs.Auth;

/// <summary>Profile update (bonus). Never carries a password - use ChangePasswordViewModel.</summary>
public class UpdateProfileViewModel
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }
}
