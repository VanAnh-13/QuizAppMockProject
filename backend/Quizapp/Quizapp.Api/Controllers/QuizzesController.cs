using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quizapp.Application.DTOs.QuizManager.QuizQuestion;
using Quizapp.Application.DTOs.QuizManager.Quizzes;
using Quizapp.Application.Services.QuizManager;

namespace Quizapp.Api.Controllers;

[ApiController]
[Route("api/quizzes")]
[Authorize(Roles = "Admin")]
public class QuizzesController(IQuizService quizService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var quizzes = await quizService.GetListAsync(pageNumber, pageSize, search, cancellationToken);

        return Ok(quizzes);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var quiz = await quizService.GetByIdAsync(id, cancellationToken);

        return Ok(quiz);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuizDto request, CancellationToken cancellationToken)
    {
        var quiz = await quizService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = quiz.Id }, quiz);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateQuizDto request,
        CancellationToken cancellationToken)
    {
        await quizService.UpdateAsync(id, request, cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        await quizService.SetActiveAsync(id, request.IsActive!.Value, cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/questions")]
    public async Task<IActionResult> AddQuestion(Guid id, [FromBody] AddQuestionToQuizDto request,
        CancellationToken cancellationToken)
    {
        request.QuizId = id;
        var result = await quizService.AddQuestionAsync(request, cancellationToken);

        return Created(string.Empty, result);
    }

    [HttpDelete("{id:guid}/questions/{questionId:guid}")]
    public async Task<IActionResult> RemoveQuestion(Guid id, Guid questionId, CancellationToken cancellationToken)
    {
        await quizService.RemoveQuestionAsync(id, questionId, cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/questions/order")]
    public async Task<IActionResult> ReorderQuestions(Guid id, [FromBody] List<Guid> orderedQuestionIds,
        CancellationToken cancellationToken)
    {
        await quizService.ReorderQuestionAsync(id, orderedQuestionIds, cancellationToken);

        return NoContent();
    }
}
