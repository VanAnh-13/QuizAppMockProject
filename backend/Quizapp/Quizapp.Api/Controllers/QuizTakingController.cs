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
    [HttpGet("quizzes/{quizId:guid}/start")]
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
