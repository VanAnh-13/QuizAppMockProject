using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApp.Application.Common.Paging;
using QuizApp.Application.DTOs.Quizzes;
using QuizApp.Application.Interfaces;

namespace QuizApp.Api.Controllers;

[ApiController]
[Route("api/quizzes")]
public class QuizzesController : ControllerBase
{
    private readonly IQuizService _quizService;

    public QuizzesController(IQuizService quizService)
    {
        _quizService = quizService;
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
}
