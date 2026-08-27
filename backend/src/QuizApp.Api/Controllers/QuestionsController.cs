using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApp.Application.Common.Paging;
using QuizApp.Application.DTOs.Answers;
using QuizApp.Application.DTOs.Questions;
using QuizApp.Application.Interfaces;

namespace QuizApp.Api.Controllers;

[ApiController]
[Route("api/questions")]
[Authorize(Policy = "RequireManager")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly IAnswerService _answerService;

    public QuestionsController(IQuestionService questionService, IAnswerService answerService)
    {
        _questionService = questionService;
        _answerService = answerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PagedQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _questionService.GetPagedAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _questionService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] QuestionCreateViewModel model, CancellationToken cancellationToken)
    {
        var question = await _questionService.CreateAsync(model, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = question.Id }, question);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] QuestionEditViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id.ToString();
        return Ok(await _questionService.UpdateAsync(model, cancellationToken));
    }

    /// <summary>Preview the delete impact: warns when the question is assigned to quizzes (UC-11 alt-flow 3.1).</summary>
    [HttpGet("{id:guid}/delete-preview")]
    public async Task<IActionResult> DeletePreview(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _questionService.GetDeletePreviewAsync(id, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _questionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{questionId:guid}/answers")]
    public async Task<IActionResult> GetAnswers(Guid questionId, CancellationToken cancellationToken)
    {
        return Ok(await _answerService.GetAnswersByQuestionIdAsync(questionId, cancellationToken));
    }

    [HttpPost("{questionId:guid}/answers")]
    public async Task<IActionResult> CreateAnswer(Guid questionId, [FromBody] AnswerCreateViewModel model, CancellationToken cancellationToken)
    {
        model.QuestionId = questionId.ToString();
        var answer = await _answerService.CreateAsync(model, cancellationToken);
        return Created($"/api/answers/{answer.Id}", answer);
    }
}
