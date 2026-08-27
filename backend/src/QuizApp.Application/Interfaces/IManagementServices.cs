using QuizApp.Application.Common.Paging;
using QuizApp.Application.DTOs.Feedback;
using QuizApp.Application.DTOs.Roles;
using QuizApp.Application.DTOs.Users;

namespace QuizApp.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserViewModel>> GetPagedAsync(PagedQuery query, CancellationToken ct = default);

    Task<UserViewModel> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<UserViewModel> CreateAsync(UserCreateViewModel model, CancellationToken ct = default);

    /// <summary>Never edits the password - ChangePassword is a separate flow.</summary>
    Task<UserViewModel> UpdateAsync(UserEditViewModel model, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<UserViewModel> SetStatusAsync(Guid id, bool isActive, CancellationToken ct = default);

    Task<UserViewModel> SetRolesAsync(Guid id, string[] roles, CancellationToken ct = default);
}

public interface IRoleService
{
    Task<PagedResult<RoleViewModel>> GetPagedAsync(PagedQuery query, CancellationToken ct = default);

    Task<IReadOnlyList<RoleViewModel>> GetAllAsync(CancellationToken ct = default);

    Task<RoleViewModel> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<RoleViewModel> CreateAsync(RoleCreateViewModel model, CancellationToken ct = default);

    Task<RoleViewModel> UpdateAsync(RoleEditViewModel model, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IFeedbackService
{
    Task SubmitAsync(FeedbackCreateViewModel model, CancellationToken ct = default);
}
