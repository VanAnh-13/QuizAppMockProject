using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApp.Application.Common.Paging;
using QuizApp.Application.DTOs.QuizCodes;
using QuizApp.Application.DTOs.Quizzes;
using QuizApp.Application.Interfaces;

namespace QuizApp.Api.Controllers;

[ApiController]
[Route("api/quizzes")]
public class QuizzesController : ControllerBase
{
    private readonly IQuizService _quizService;
    private readonly IQuizCodeService _codeService;
    private readonly IQuizAttemptService _attemptService;

    public QuizzesController(
        IQuizService quizService,
        IQuizCodeService codeService,
        IQuizAttemptService attemptService)
    {
        _quizService = quizService;
        _codeService = codeService;
        _attemptService = attemptService;
    }

    /// <summary>Paged quiz list (anonymous). Managers get all quizzes, the public only active ones.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        var isManager = User.IsInRole("Admin") || User.IsInRole("Editor");
        return Ok(await _quizService.GetPagedAsync(query, activeOnly: !isManager, cancellationToken));
    }

    /// <summary>Active quizzes for the Home page (UC-01).</summary>
    [AllowAnonymous]
    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        return Ok(await _quizService.GetActiveAsync(cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _quizService.GetByIdAsync(id, cancellationToken));
    }

    [Authorize(Policy = "RequireManager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] QuizCreateViewModel model, CancellationToken cancellationToken)
    {
        var quiz = await _quizService.CreateAsync(model, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = quiz.Id }, quiz);
    }

    [Authorize(Policy = "RequireManager")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] QuizEditViewModel model, CancellationToken cancellationToken)
    {
        if (model.Id != id.ToString())
        {
            model.Id = id.ToString();
        }
        return Ok(await _quizService.UpdateAsync(model, cancellationToken));
    }

    [Authorize(Policy = "RequireManager")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _quizService.DeleteAsync(id, cancellationToken);
        return Ok(result);
    }

    [Authorize(Policy = "RequireManager")]
    [HttpGet("{quizId:guid}/questions")]
    public async Task<IActionResult> GetQuestions(Guid quizId, CancellationToken cancellationToken)
    {
        return Ok(await _quizService.GetQuestionsByQuizIdAsync(quizId, cancellationToken));
    }

    [Authorize(Policy = "RequireManager")]
    [HttpPost("{quizId:guid}/questions")]
    public async Task<IActionResult> AddQuestion(Guid quizId, [FromBody] QuizQuestionCreateViewModel model, CancellationToken cancellationToken)
    {
        model.QuizId = quizId.ToString();
        await _quizService.AddQuestionToQuizAsync(model, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "RequireManager")]
    [HttpDelete("{quizId:guid}/questions/{quizQuestionId:guid}")]
    public async Task<IActionResult> RemoveQuestion(Guid quizId, Guid quizQuestionId, CancellationToken cancellationToken)
    {
        await _quizService.RemoveQuestionFromQuizAsync(quizId, quizQuestionId, cancellationToken);
        return NoContent();
    }

    // ---- Quiz codes (T4) -----------------------------------------------

    /// <summary>
    /// Self-issue a code for the authenticated user (Start button path).
    /// Returns an existing unused code if one already exists for (quiz, user).
    /// </summary>
    [Authorize]
    [HttpPost("{quizId:guid}/codes/self")]
    public async Task<IActionResult> SelfIssueCode(Guid quizId, CancellationToken cancellationToken)
    {
        var userId = GetCallerId();
        var result = await _codeService.SelfIssueAsync(quizId, userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Bulk-generate codes for a quiz + list of users (admin session).</summary>
    [Authorize(Policy = "RequireManager")]
    [HttpPost("{quizId:guid}/codes/bulk")]
    public async Task<IActionResult> BulkGenerateCodes(
        Guid quizId,
        [FromBody] BulkCodeRequestViewModel request,
        CancellationToken cancellationToken)
    {
        request.QuizId = quizId.ToString();
        var results = await _codeService.BulkGenerateAsync(request, cancellationToken);
        return Ok(results);
    }

    // ---- Quiz-taking (T4) ---------------------------------------------

    [Authorize]
    [HttpPost("prepare")]
    public async Task<IActionResult> Prepare([FromBody] PrepareQuizViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetCallerId();
        var result = await _attemptService.PrepareAsync(model, userId, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("take")]
    public async Task<IActionResult> Take([FromBody] TakeQuizViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetCallerId();
        var result = await _attemptService.TakeAsync(model, userId, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] QuizSubmissionViewModel model, CancellationToken cancellationToken)
    {
        var userId = GetCallerId();
        var result = await _attemptService.SubmitAsync(model, userId, cancellationToken);
        return Ok(result);
    }

    // ---- Helpers -------------------------------------------------------

    private Guid GetCallerId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException("User identity not found in token.");
        return Guid.Parse(sub);
    }
}

