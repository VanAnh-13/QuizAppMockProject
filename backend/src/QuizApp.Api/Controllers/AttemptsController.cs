using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApp.Application.Interfaces;

namespace QuizApp.Api.Controllers;

[ApiController]
[Route("api/attempts")]
[Authorize]
public class AttemptsController : ControllerBase
{
    private readonly IQuizAttemptService _attemptService;

    public AttemptsController(IQuizAttemptService attemptService)
    {
        _attemptService = attemptService;
    }

    /// <summary>Quiz attempt history for the authenticated user (UC-04 / UC-06).</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyAttempts(CancellationToken cancellationToken)
    {
        var userId = GetCallerId();
        return Ok(await _attemptService.GetMyAttemptsAsync(userId, cancellationToken));
    }

    /// <summary>
    /// Full attempt detail with per-question breakdown.
    /// The attempt owner or any manager may call this.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAttemptDetail(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCallerId();
        var isManager = User.IsInRole("Admin") || User.IsInRole("Editor");
        return Ok(await _attemptService.GetAttemptDetailAsync(id, userId, isManager, cancellationToken));
    }

    private Guid GetCallerId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException("User identity not found in token.");
        return Guid.Parse(sub);
    }
}
