using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quizapp.Application.DTOs.Contact;
using Quizapp.Application.Services.Contact;

namespace Quizapp.Api.Controllers;

[ApiController]
[Route("api/public/contact")]
[AllowAnonymous]
public class ContactController(IContactService contact) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] ContactMessageDto request, CancellationToken cancellationToken)
    {
        var receipt = await contact.SendAsync(request, cancellationToken);
        return Ok(receipt);
    }
}
