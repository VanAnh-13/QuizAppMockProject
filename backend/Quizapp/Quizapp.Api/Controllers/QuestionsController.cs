using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Application.Services.QuestionManager;

namespace Quizapp.Api.Controllers;

[ApiController]
[Route("api/questions")]
[Authorize(Roles = "Admin")]
public class QuestionsController(IQuestionService questionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var questions = await questionService.GetListAsync(pageNumber, pageSize, search, cancellationToken);

        return Ok(questions);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var question = await questionService.GetByIdAsync(id, cancellationToken);

        return Ok(question);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuestionDto request, CancellationToken cancellationToken)
    {
        var question = await questionService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = question.Id }, question);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateQuestionDto request,
        CancellationToken cancellationToken)
    {
        await questionService.UpdateAsync(id, request, cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        await questionService.SetActiveAsync(id, request.IsActive, cancellationToken);

        return NoContent();
    }
}
