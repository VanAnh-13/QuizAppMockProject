using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApp.Application.DTOs.Feedback;
using QuizApp.Application.Interfaces;

namespace QuizApp.Api.Controllers;

[ApiController]
[Route("api/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;

    public FeedbackController(IFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    /// <summary>Contact form submission (UC-02) - stored + logged, no SMTP.</summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] FeedbackCreateViewModel model, CancellationToken cancellationToken)
    {
        await _feedbackService.SubmitAsync(model, cancellationToken);
        return Ok(new { message = "Thank you for your feedback." });
    }
}
