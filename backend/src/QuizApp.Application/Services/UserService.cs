using System.Linq.Expressions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuizApp.Application.Common;
using QuizApp.Application.Common.Exceptions;
using QuizApp.Application.Common.Paging;
using QuizApp.Application.DTOs.Users;
using QuizApp.Application.Interfaces;
using QuizApp.Domain.Entities;

namespace QuizApp.Application.Services;

public class UserService : IUserService
{
    private static readonly IReadOnlyDictionary<string, Expression<Func<ApplicationUser, object?>>> Sortable =
        new Dictionary<string, Expression<Func<ApplicationUser, object?>>>
        {
            ["username"] = u => u.UserName,
            ["email"] = u => u.Email,
            ["firstname"] = u => u.FirstName,
            ["lastname"] = u => u.LastName,
            ["isactive"] = u => u.IsActive
        };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly IValidator<UserCreateViewModel>? _createValidator;
    private readonly IValidator<UserEditViewModel>? _editValidator;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        IValidator<UserCreateViewModel>? createValidator = null,
        IValidator<UserEditViewModel>? editValidator = null)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _createValidator = createValidator;
        _editValidator = editValidator;
    }

    public async Task<PagedResult<UserViewModel>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        IQueryable<ApplicationUser> users = _userManager.Users;
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            users = users.Where(u => (u.UserName ?? string.Empty).Contains(search)
                                     || (u.Email ?? string.Empty).Contains(search)
                                     || u.FirstName.Contains(search)
                                     || u.LastName.Contains(search));
        }

        var page = Math.Max(1, query.Page);
        var pageSize = query.PageSize <= 0
            ? _configuration.GetValue("Paging:DefaultPageSize", 10)
            : Math.Min(query.PageSize, _configuration.GetValue("Paging:MaxPageSize", 100));

        var sortKey = !string.IsNullOrWhiteSpace(query.SortBy) && Sortable.ContainsKey(query.SortBy.ToLowerInvariant())
            ? query.SortBy.ToLowerInvariant()
            : "username";
        var selector = Sortable[sortKey];
        var sorted = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase)
            ? users.OrderByDescending(selector)
            : users.OrderBy(selector);

        var totalItems = await users.CountAsync(ct);
        var slice = await sorted.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var mapped = new List<UserViewModel>(slice.Count);
        foreach (var user in slice)
        {
            var roles = await _userManager.GetRolesAsync(user);
            mapped.Add(AuthService.Map(user, roles));
        }

        return PagedResult<UserViewModel>.Create(mapped, totalItems, page, pageSize);
    }

    public async Task<UserViewModel> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
                   ?? throw new NotFoundException("User not found.");
        return AuthService.Map(user, await _userManager.GetRolesAsync(user));
    }

    public async Task<UserViewModel> CreateAsync(UserCreateViewModel model, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(_createValidator, model, ct);

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
            throw new ConflictException("User could not be created.", fieldErrors);
        }

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DateOfBirth = model.DateOfBirth?.Date,
            IsActive = model.IsActive
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(user, "User");
        return AuthService.Map(user, await _userManager.GetRolesAsync(user));
    }

    public async Task<UserViewModel> UpdateAsync(UserEditViewModel model, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(_editValidator, model, ct);

        var user = await _userManager.FindByIdAsync(GuidParsing.Parse(model.Id, "user").ToString())
                   ?? throw new NotFoundException("User not found.");

        if (!string.Equals(user.UserName, model.UserName, StringComparison.OrdinalIgnoreCase)
            && await _userManager.FindByNameAsync(model.UserName) is not null)
        {
            throw new ConflictException("User could not be updated.",
                new Dictionary<string, string[]> { ["userName"] = new[] { "This username is already taken." } });
        }
        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase)
            && await _userManager.FindByEmailAsync(model.Email) is not null)
        {
            throw new ConflictException("User could not be updated.",
                new Dictionary<string, string[]> { ["email"] = new[] { "This email is already registered." } });
        }

        user.UserName = model.UserName;
        user.Email = model.Email;
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.PhoneNumber = model.PhoneNumber;
        user.DateOfBirth = model.DateOfBirth?.Date;
        user.IsActive = model.IsActive;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return await SaveRolesAsync(user, model.Roles, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
                   ?? throw new NotFoundException("User not found.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task<UserViewModel> SetStatusAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
                   ?? throw new NotFoundException("User not found.");

        user.IsActive = isActive;
        await _userManager.UpdateAsync(user);

        return AuthService.Map(user, await _userManager.GetRolesAsync(user));
    }

    public async Task<UserViewModel> SetRolesAsync(Guid id, string[] roles, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString())
                   ?? throw new NotFoundException("User not found.");

        return await SaveRolesAsync(user, roles, ct);
    }

    private async Task<UserViewModel> SaveRolesAsync(ApplicationUser user, string[] roles, CancellationToken ct)
    {
        var normalized = roles.Select(r => r.Trim()).Where(r => r.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var role in normalized)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                throw new BadRequestException($"Role '{role}' does not exist.",
                    new Dictionary<string, string[]> { ["roles"] = new[] { $"Role '{role}' does not exist." } });
            }
        }

        var current = await _userManager.GetRolesAsync(user);
        var toRemove = current.Except(normalized, StringComparer.OrdinalIgnoreCase).ToArray();
        var toAdd = normalized.Except(current, StringComparer.OrdinalIgnoreCase).ToArray();
        if (toRemove.Length > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, toRemove);
        }
        if (toAdd.Length > 0)
        {
            await _userManager.AddToRolesAsync(user, toAdd);
        }

        return AuthService.Map(user, normalized);
    }
}
