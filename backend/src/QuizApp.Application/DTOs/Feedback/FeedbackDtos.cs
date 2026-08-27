namespace QuizApp.Application.DTOs.Feedback;

/// <summary>Contact form payload (UC-02). Stored + logged, no SMTP.</summary>
public class FeedbackCreateViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
}
