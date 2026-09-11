using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quizapp.Application.Services.QuizTaking;

namespace Quizapp.Api.Controllers;

[ApiController]
[Route("api/public/quizzes")]
[AllowAnonymous]
public class PublicQuizzesController(IPublicQuizCatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActiveQuizzes([FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20, [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var quizzes = await catalog.GetActiveQuizzesAsync(pageNumber, pageSize, search, cancellationToken);

        return Ok(quizzes);
    }
}
