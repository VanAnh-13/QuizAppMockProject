using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using QuizApp.Application.Common.Exceptions;
using QuizApp.Application.DTOs.Auth;
using QuizApp.Application.Interfaces;

namespace QuizApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IFileStorage _fileStorage;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IFileStorage fileStorage, IConfiguration configuration)
    {
        _authService = authService;
        _fileStorage = fileStorage;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterViewModel model, CancellationToken cancellationToken)
    {
        var user = await _authService.RegisterAsync(model, cancellationToken);
        return CreatedAtAction(nameof(Me), user);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginViewModel model, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(model, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await _authService.GetCurrentUserAsync(CurrentUserId(), cancellationToken);
        return Ok(user);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        await _authService.ChangePasswordAsync(CurrentUserId(), model, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileViewModel model, CancellationToken cancellationToken)
    {
        var user = await _authService.UpdateProfileAsync(CurrentUserId(), model, cancellationToken);
        return Ok(user);
    }

    [Authorize]
    [HttpPost("avatar")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new BadRequestException("Please choose an image file to upload.");
        }

        var maxBytes = _configuration.GetValue("FileStorage:MaxAvatarBytes", 2L * 1024 * 1024);
        if (file.Length > maxBytes)
        {
            throw new BadRequestException($"The image is too large. Maximum size is {maxBytes / 1024 / 1024} MB.");
        }

        await using var stream = file.OpenReadStream();
        var url = await _fileStorage.SaveAsync(file.FileName, file.ContentType, stream, cancellationToken);
        await _authService.SetAvatarAsync(CurrentUserId(), url, cancellationToken);

        return Ok(new { avatar = url });
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedException("Invalid token.");
    }
}
