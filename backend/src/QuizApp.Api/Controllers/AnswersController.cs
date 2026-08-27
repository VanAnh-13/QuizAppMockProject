using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApp.Application.DTOs.Answers;
using QuizApp.Application.Interfaces;

namespace QuizApp.Api.Controllers;

[ApiController]
[Route("api/answers")]
[Authorize(Policy = "RequireManager")]
public class AnswersController : ControllerBase
{
    private readonly IAnswerService _answerService;

    public AnswersController(IAnswerService answerService)
    {
        _answerService = answerService;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _answerService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AnswerEditViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id.ToString();
        return Ok(await _answerService.UpdateAsync(model, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _answerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
