namespace Quizapp.Application.Abstractions.Authentication;

public sealed class PasswordResetOptions
{
    public const string SectionName = "PasswordReset";
    public const int TokenLifetimeMinutes = 15;
    public const int RequestCooldownSeconds = 60;
    public const int QueueCapacity = 100;

    public string FrontendUrl { get; set; } = string.Empty;
}
