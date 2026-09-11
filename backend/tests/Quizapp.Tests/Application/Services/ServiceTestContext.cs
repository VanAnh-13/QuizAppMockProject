using Microsoft.Extensions.DependencyInjection;
using Quizapp.Application;
using Quizapp.Application.Abstractions.Authentication;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Common;
using Quizapp.Application.Services.Common;
using Quizapp.Domain.Entities;
using Quizapp.Infrastructure.Authentication;

namespace Quizapp.Tests.Application.Services;

internal sealed class ServiceTestContext : IDisposable
{
    public TestCurrentUser CurrentUser { get; } = new();
    public MemoryUsers Users { get; } = new();
    public MemoryQuizzes Quizzes { get; } = new();
    public MemoryQuestions Questions { get; } = new();
    public MemoryRoles Roles { get; }
    public ServiceCollection Services { get; } = new();

    private ServiceProvider? _provider;
    private IServiceScope? _scope;

    public ServiceTestContext()
    {
        Roles = new MemoryRoles(Users);

        var administrator = new User
        {
            Id = Guid.NewGuid(), Username = "admin", Email = "admin@example.com",
            Password = "test-only-hash", IsActive = true
        };

        var adminRole = new Role
            { Id = Guid.NewGuid(), RoleName = ServiceAuthorization.AdministratorRole, IsActive = true };

        administrator.Roles.Add(adminRole);
        Users.Add(administrator);
        Roles.Add(adminRole);
        CurrentUser.UserId = administrator.Id;
        Services.AddApplication();
        Services.AddSingleton<ICurrentUser>(CurrentUser);
        Services.AddSingleton<IUserRepository>(Users);
        Services.AddSingleton<IRoleRepository>(Roles);
        Services.AddSingleton<IQuizRepository>(Quizzes);
        Services.AddSingleton<IQuestionRepository>(Questions);
        Services.AddSingleton<IQuizAttemptRepository, MemoryQuizAttempts>();
        Services.AddSingleton<IUnitOfWork, MemoryUnitOfWork>();
        Services.AddSingleton<IPasswordService, IdentityPasswordService>();
        Services.AddSingleton<ITokenService, JwtTokenService>();

        Services.Configure<JwtOptions>(options =>
        {
            options.Issuer = "quizapp-tests";
            options.Audience = "quizapp-test-client";

            options.SigningKey =
                Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        });
    }

    public T Get<T>() where T : notnull
    {
        _provider ??= Services.BuildServiceProvider();
        _scope ??= _provider.CreateScope();

        return _scope.ServiceProvider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _provider?.Dispose();
    }
}

internal sealed class TestCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }
}

internal sealed class MemoryQuizzes() : MemoryRepository<Quiz>(quiz => quiz.Id, quiz => quiz.Title), IQuizRepository
{
    public Task<PagedResultDto<Quiz>> GetActiveListAsync(int pageNumber, int pageSize, string? search,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = Rows.Where(quiz => quiz.IsActive)
            .Where(quiz => search is null || quiz.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(quiz => quiz.Title)
            .ThenBy(quiz => quiz.Id)
            .ToArray();

        return Task.FromResult(new PagedResultDto<Quiz>
        {
            Items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray(),
            TotalCount = query.Length,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }
}

internal sealed class MemoryQuestions()
    : MemoryRepository<Question>(question => question.Id, question => question.Content), IQuestionRepository;

internal sealed class MemoryQuizAttempts : IQuizAttemptRepository
{
    public List<QuizAttempt> Rows { get; } = [];

    public Task<PagedResultDto<QuizAttempt>> GetInProgressAsync(Guid userId, int pageNumber, int pageSize,
        Guid? quizId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = Rows.Where(a => a.UserId == userId && a.SubmitAt == null && (quizId is null || a.QuizId == quizId))
            .OrderByDescending(a => a.StartedAt).ThenBy(a => a.Id).ToArray();
        return Task.FromResult(new PagedResultDto<QuizAttempt>
        {
            Items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray(),
            TotalCount = query.Length, PageNumber = pageNumber, PageSize = pageSize
        });
    }

    public Task<QuizAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Rows.SingleOrDefault(a => a.Id == id));
    }

    public Task<PagedResultDto<QuizAttempt>> GetHistoryAsync(Guid userId, int pageNumber, int pageSize, Guid? quizId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = Rows.Where(a => a.UserId == userId && a.SubmitAt != null && (quizId is null || a.QuizId == quizId))
            .OrderByDescending(a => a.SubmitAt)
            .ToArray();

        return Task.FromResult(new PagedResultDto<QuizAttempt>
        {
            Items = query.Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToArray(),
            TotalCount = query.Length, PageNumber = pageNumber, PageSize = pageSize
        });
    }

    public void Add(QuizAttempt attempt) => Rows.Add(attempt);
}

internal sealed class MemoryUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }
}

internal abstract class MemoryRepository<T>(Func<T, Guid> id, Func<T, string> text) : IRepository<T> where T : class
{
    public List<T> Rows { get; } = [];

    public Task<T?> GetByIdAsync(Guid entityId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Rows.SingleOrDefault(row => id(row) == entityId));
    }

    public Task<PagedResultDto<T>> GetListAsync(int pageNumber, int pageSize, string? search,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = Rows.Where(row => search is null || text(row)
                .Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(text)
            .ThenBy(id)
            .ToArray();

        return Task.FromResult(new PagedResultDto<T>
        {
            Items = query.Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToArray(),
            TotalCount = query.Length, PageNumber = pageNumber, PageSize = pageSize
        });
    }

    public void Add(T entity) => Rows.Add(entity);

    public void Remove(T entity) => Rows.Remove(entity);
}

internal sealed class MemoryUsers() : MemoryRepository<User>(user => user.Id, user => user.Username), IUserRepository
{
    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Rows.SingleOrDefault(user =>
            string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<bool> UsernameExistsAsync(string username, Guid? excludedId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Rows.Any(user =>
            user.Id != excludedId && string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludedId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Rows.Any(user =>
            user.Id != excludedId && string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));
    }
}

internal sealed class MemoryRoles(MemoryUsers users)
    : MemoryRepository<Role>(role => role.Id, role => role.RoleName), IRoleRepository
{
    public Task<bool> NameExistsAsync(string name, Guid? excludedId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Rows.Any(role =>
            role.Id != excludedId && string.Equals(role.RoleName, name, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<IReadOnlyList<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IReadOnlyList<Role>>(Rows.Where(role => ids.Contains(role.Id))
            .ToArray());
    }

    public Task<bool> HasUsersAsync(Guid roleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(users.Rows.Any(user => user.Roles.Any(role => role.Id == roleId)));
    }
}
