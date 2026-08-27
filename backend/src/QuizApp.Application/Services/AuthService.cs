using FluentValidation;
using Microsoft.AspNetCore.Identity;
using QuizApp.Application.Common;
using QuizApp.Application.Common.Exceptions;
using QuizApp.Application.DTOs.Auth;
using QuizApp.Application.DTOs.Users;
using QuizApp.Application.Interfaces;
using QuizApp.Domain.Entities;

namespace QuizApp.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IValidator<RegisterViewModel>? _registerValidator;
    private readonly IValidator<LoginViewModel>? _loginValidator;
    private readonly IValidator<ChangePasswordViewModel>? _changePasswordValidator;
    private readonly IValidator<UpdateProfileViewModel>? _updateProfileValidator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        IValidator<RegisterViewModel>? registerValidator = null,
        IValidator<LoginViewModel>? loginValidator = null,
        IValidator<ChangePasswordViewModel>? changePasswordValidator = null,
        IValidator<UpdateProfileViewModel>? updateProfileValidator = null)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _changePasswordValidator = changePasswordValidator;
        _updateProfileValidator = updateProfileValidator;
    }

    public async Task<UserViewModel> RegisterAsync(RegisterViewModel model, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAsync(_registerValidator, model, cancellationToken);

        // Duplicate username/email produce field-level 409 errors, never a stack trace.
        var fieldErrors = new Dictionary<string, string[]>();
        if (await _userManager.FindByNameAsync(model.UserName) is not null)
        {
            fieldErrors["userName"] = new[] { "This username is already taken." };
        }
        if (await _userManager.FindByEmailAsync(model.Email) is not null)
        {
            fieldErrors["email"] = new[] { "This email is already registered." };
        }
        if (fieldErrors.Count > 0)
        {
            throw new ConflictException("Account could not be created.", fieldErrors);
        }

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DateOfBirth = model.DateOfBirth?.Date,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code.Contains("Password", StringComparison.OrdinalIgnoreCase) ? "password" : "userName")
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            throw new BadRequestException("Registration failed.", errors);
        }

        await _userManager.AddToRoleAsync(user, "User");

        var roles = await _userManager.GetRolesAsync(user);
        return Map(user, roles);
    }

    public async Task<LoginResponseViewModel> LoginAsync(LoginViewModel model, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAsync(_loginValidator, model, cancellationToken);

        var user = await _userManager.FindByNameAsync(model.UserName)
                   ?? await _userManager.FindByEmailAsync(model.UserName);

        if (user is null || !await _userManager.CheckPasswordAsync(user, model.Password))
        {
            throw new UnauthorizedException("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("This account has been deactivated. Please contact an administrator.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expires) = _jwtTokenService.CreateToken(user, roles);

        return new LoginResponseViewModel
        {
            UserInformation = Map(user, roles),
            Token = token,
            Expires = expires
        };
    }

    public async Task<UserViewModel> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User not found.");

        var roles = await _userManager.GetRolesAsync(user);
        return Map(user, roles);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordViewModel model, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAsync(_changePasswordValidator, model, cancellationToken);

        var user = await _userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User not found.");

        if (!await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
        {
            throw new BadRequestException("The current password is incorrect.",
                new Dictionary<string, string[]> { ["currentPassword"] = new[] { "The current password is incorrect." } });
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task<UserViewModel> UpdateProfileAsync(Guid userId, UpdateProfileViewModel model, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAsync(_updateProfileValidator, model, cancellationToken);

        var user = await _userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User not found.");

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.PhoneNumber = model.PhoneNumber;
        user.DateOfBirth = model.DateOfBirth?.Date;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Map(user, roles);
    }

    public async Task<string> SetAvatarAsync(Guid userId, string avatarUrl, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
                   ?? throw new NotFoundException("User not found.");

        user.AvatarUrl = avatarUrl;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return avatarUrl;
    }

    public static UserViewModel Map(ApplicationUser user, IEnumerable<string> roles) => new()
    {
        Id = user.Id.ToString(),
        FirstName = user.FirstName,
        LastName = user.LastName,
        DisplayName = user.DisplayName,
        Email = user.Email ?? string.Empty,
        UserName = user.UserName ?? string.Empty,
        PhoneNumber = user.PhoneNumber ?? string.Empty,
        DateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd") ?? string.Empty,
        Avatar = user.AvatarUrl ?? string.Empty,
        IsActive = user.IsActive,
        Roles = roles.ToArray()
    };
}
