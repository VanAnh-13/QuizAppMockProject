using Quizapp.Application.DTOs.Authentication;

namespace Quizapp.Api.Controllers;

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quizapp.Application.DTOs;
using Quizapp.Application.Services.Authentication;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("forgot-password")]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request,
        CancellationToken cancellationToken)
    {
        await authService.RequestPasswordResetAsync(request, cancellationToken);
        return Accepted(new { message = "If an active account matches this email, you will receive a password reset link." });
    }

    [AllowAnonymous]
    [EnableRateLimiting("reset-password")]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request,
        CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto request, CancellationToken cancellationToken)
    {
        var user = await authService.RegisterAsync(request, cancellationToken);

        return Created(string.Empty, user);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);

        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var user = await authService.GetCurrentUserAsync(cancellationToken);

        return Ok(user);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request,
        CancellationToken cancellationToken)
    {
        await authService.ChangePasswordAsync(request, cancellationToken);

        return NoContent();
    }
}
