using System.Linq.Expressions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuizApp.Application.Common;
using QuizApp.Application.Common.Exceptions;
using QuizApp.Application.Common.Paging;
using QuizApp.Application.DTOs.Roles;
using QuizApp.Application.Interfaces;
using QuizApp.Domain.Entities;

namespace QuizApp.Application.Services;

public class RoleService : IRoleService
{
    private static readonly IReadOnlyDictionary<string, Expression<Func<ApplicationRole, object?>>> Sortable =
        new Dictionary<string, Expression<Func<ApplicationRole, object?>>>
        {
            ["name"] = r => r.Name,
            ["description"] = r => r.Description,
            ["isactive"] = r => r.IsActive
        };

    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly IValidator<RoleCreateViewModel>? _createValidator;
    private readonly IValidator<RoleEditViewModel>? _editValidator;

    public RoleService(
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        IValidator<RoleCreateViewModel>? createValidator = null,
        IValidator<RoleEditViewModel>? editValidator = null)
    {
        _roleManager = roleManager;
        _configuration = configuration;
        _createValidator = createValidator;
        _editValidator = editValidator;
    }

    public Task<PagedResult<RoleViewModel>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        IQueryable<ApplicationRole> roles = _roleManager.Roles;
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            roles = roles.Where(r => (r.Name ?? string.Empty).Contains(search)
                                     || (r.Description ?? string.Empty).Contains(search));
        }

        return roles.ToPagedResultAsync(query, Sortable, "name", Map,
            _configuration.GetValue("Paging:DefaultPageSize", 10),
            _configuration.GetValue("Paging:MaxPageSize", 100), ct);
    }

    public async Task<IReadOnlyList<RoleViewModel>> GetAllAsync(CancellationToken ct = default)
    {
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync(ct);
        return roles.Select(Map).ToList();
    }

    public async Task<RoleViewModel> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString())
                   ?? throw new NotFoundException("Role not found.");
        return Map(role);
    }

    public async Task<RoleViewModel> CreateAsync(RoleCreateViewModel model, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(_createValidator, model, ct);

        if (await _roleManager.RoleExistsAsync(model.Name))
        {
            throw new ConflictException("Role could not be created.",
                new Dictionary<string, string[]> { ["name"] = new[] { "This role name already exists." } });
        }

        var role = new ApplicationRole(model.Name.Trim(), model.Description.Trim()) { IsActive = model.IsActive };
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return Map(role);
    }

    public async Task<RoleViewModel> UpdateAsync(RoleEditViewModel model, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(_editValidator, model, ct);

        var role = await _roleManager.FindByIdAsync(GuidParsing.Parse(model.Id, "role").ToString())
                   ?? throw new NotFoundException("Role not found.");

        role.Name = model.Name.Trim();
        role.Description = model.Description.Trim();
        role.IsActive = model.IsActive;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return Map(role);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString())
                   ?? throw new NotFoundException("Role not found.");

        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("The built-in Admin role cannot be deleted.");
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
    }

    private static RoleViewModel Map(ApplicationRole role) => new()
    {
        Id = role.Id.ToString(),
        Name = role.Name ?? string.Empty,
        Description = role.Description ?? string.Empty,
        IsActive = role.IsActive
    };
}
