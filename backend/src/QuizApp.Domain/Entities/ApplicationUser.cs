using Microsoft.AspNetCore.Identity;

namespace QuizApp.Domain.Entities;

/// <summary>
/// Identity user extended with the SRS profile fields.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string DisplayName => $"{FirstName} {LastName}".Trim();

    // Navigation properties
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
    public ICollection<QuizCode> QuizCodes { get; set; } = new List<QuizCode>();
}
