namespace Quizapp.Application.DTOs.Authentication;

public sealed class ResetPasswordDto
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
