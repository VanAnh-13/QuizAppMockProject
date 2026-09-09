using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Application.Services.QuizTaking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Quizapp.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class QuizTakingController(IQuizAttemptService attemptService) : ControllerBase
{
    [HttpPost("attempts/{attemptId:guid}/submit")]
    public async Task<IActionResult> SubmitSaved(Guid attemptId, [FromBody] AttemptRevisionDto request,
        CancellationToken cancellationToken) =>
        Ok(await attemptService.SubmitSavedAsync(attemptId, request, cancellationToken));

    [HttpGet("attempts/in-progress")]
    public async Task<IActionResult> GetInProgress([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? quizId = null, CancellationToken cancellationToken = default) =>
        Ok(await attemptService.GetInProgressAsync(pageNumber, pageSize, quizId, cancellationToken));

    [HttpPost("attempts/{attemptId:guid}/pause")]
    public async Task<IActionResult> Pause(Guid attemptId, [FromBody] SaveQuizProgressDto request,
        CancellationToken cancellationToken) =>
        Ok(await attemptService.PauseAsync(attemptId, request, cancellationToken));

    [HttpPost("attempts/{attemptId:guid}/resume")]
    public async Task<IActionResult> Resume(Guid attemptId, [FromBody] AttemptRevisionDto request,
        CancellationToken cancellationToken) =>
        Ok(await attemptService.ResumeAsync(attemptId, request, cancellationToken));

    [HttpGet("attempts/{attemptId:guid}/progress")]
    public async Task<IActionResult> GetProgress(Guid attemptId, CancellationToken cancellationToken) =>
        Ok(await attemptService.GetProgressAsync(attemptId, cancellationToken));

    [HttpPut("attempts/{attemptId:guid}/progress")]
    public async Task<IActionResult> SaveProgress(Guid attemptId, [FromBody] SaveQuizProgressDto request,
        CancellationToken cancellationToken) =>
        Ok(await attemptService.SaveProgressAsync(attemptId, request, cancellationToken));

    [HttpPost("quizzes/{quizId:guid}/start")]
    public async Task<IActionResult> Start(Guid quizId, CancellationToken cancellationToken)
    {
        var result = await attemptService.StartAsync(quizId, cancellationToken);

        return Ok(result);
    }

    [HttpPost("quizzes/{quizId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid quizId, [FromBody] SubmitQuizDto request,
        CancellationToken cancellationToken)
    {
        var result = await attemptService.SubmitAsync(quizId, request, cancellationToken);

        return Ok(result);
    }

    [HttpGet("attempts/{attemptId:guid}")]
    public async Task<IActionResult> GetResult(Guid attemptId, CancellationToken cancellationToken)
    {
        var result = await attemptService.GetResultAsync(attemptId, cancellationToken);

        return Ok(result);
    }

    [HttpGet("quiz-history")]
    public async Task<IActionResult> GetHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? quizId = null, CancellationToken cancellationToken = default)
    {
        var history = await attemptService.GetHistoryAsync(pageNumber, pageSize, quizId, cancellationToken);

        return Ok(history);
    }
}
