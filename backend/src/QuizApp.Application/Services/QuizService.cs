using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuizApp.Application.Common;
using QuizApp.Application.Common.Exceptions;
using QuizApp.Application.Common.Paging;
using QuizApp.Application.DTOs.Questions;
using QuizApp.Application.DTOs.Quizzes;
using QuizApp.Application.Interfaces;
using QuizApp.Domain.Entities;

namespace QuizApp.Application.Services;

public class QuizService : IQuizService
{
    private static readonly IReadOnlyDictionary<string, Expression<Func<Quiz, object?>>> Sortable =
        new Dictionary<string, Expression<Func<Quiz, object?>>>
        {
            ["title"] = q => q.Title,
            ["duration"] = q => q.Duration,
            ["isactive"] = q => q.IsActive,
            ["createdat"] = q => q.CreatedAt
        };

    private readonly IAppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IValidator<QuizCreateViewModel>? _createValidator;
    private readonly IValidator<QuizEditViewModel>? _editValidator;

    public QuizService(
        IAppDbContext context,
        IConfiguration configuration,
        IValidator<QuizCreateViewModel>? createValidator = null,
        IValidator<QuizEditViewModel>? editValidator = null)
    {
        _context = context;
        _configuration = configuration;
        _createValidator = createValidator;
        _editValidator = editValidator;
    }

    public Task<PagedResult<QuizViewModel>> GetPagedAsync(PagedQuery query, bool activeOnly, CancellationToken ct = default)
    {
        IQueryable<Quiz> quizzes = _context.Quizzes;
        if (activeOnly)
        {
            quizzes = quizzes.Where(q => q.IsActive);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            quizzes = quizzes.Where(q => q.Title.Contains(search) || q.Description.Contains(search));
        }

        return quizzes.ToPagedResultAsync(query, Sortable, "title", Map, PageSize(), MaxPageSize(), ct);
    }

    public async Task<IReadOnlyList<QuizViewModel>> GetActiveAsync(CancellationToken ct = default)
    {
        var quizzes = await _context.Quizzes
            .Where(q => q.IsActive)
            .OrderBy(q => q.Title)
            .ToListAsync(ct);

        return quizzes.Select(Map).ToList();
    }

    public async Task<QuizViewModel> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var quiz = await _context.Quizzes.FindAsync(new object[] { id }, ct)
                   ?? throw new NotFoundException("Quiz not found.");
        return Map(quiz);
    }

    public async Task<QuizViewModel> CreateAsync(QuizCreateViewModel model, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(_createValidator, model, ct);
        if (model.IsActive)
        {
            throw new ConflictException("A quiz cannot be published while it has zero questions.");
        }

        var quiz = new Quiz
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Duration = model.Duration,
            IsActive = false
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync(ct);
        return Map(quiz);
    }

    public async Task<QuizViewModel> UpdateAsync(QuizEditViewModel model, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(_editValidator, model, ct);

        var quiz = await _context.Quizzes
                       .Include(q => q.QuizQuestions)
                       .FirstOrDefaultAsync(q => q.Id == GuidParsing.Parse(model.Id, "quiz"), ct)
                   ?? throw new NotFoundException("Quiz not found.");

        // UC-10 alt-flow 9.1: cannot activate/publish a quiz with zero questions.
        if (model.IsActive && quiz.QuizQuestions.Count == 0)
        {
            throw new ConflictException("A quiz cannot be published while it has zero questions.");
        }

        quiz.Title = model.Title.Trim();
        quiz.Description = model.Description.Trim();
        quiz.Duration = model.Duration;
        quiz.IsActive = model.IsActive;
        quiz.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(ct);
        return Map(quiz);
    }

    public async Task<QuizDeleteResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var quiz = await _context.Quizzes
                       .Include(q => q.QuizQuestions)
                       .FirstOrDefaultAsync(q => q.Id == id, ct)
                   ?? throw new NotFoundException("Quiz not found.");

        var hasAttempts = await _context.QuizAttempts.AnyAsync(a => a.QuizId == id, ct);
        if (hasAttempts)
        {
            // History must survive: deactivate instead of deleting.
            quiz.IsActive = false;
            quiz.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(ct);
            return new QuizDeleteResult
            {
                HardDeleted = false,
                Message = "The quiz has existing attempts, so it was deactivated instead of deleted."
            };
        }

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync(ct);
        return new QuizDeleteResult { HardDeleted = true, Message = "Quiz deleted." };
    }

    public async Task<IReadOnlyList<AssignedQuestionViewModel>> GetQuestionsByQuizIdAsync(Guid quizId, CancellationToken ct = default)
    {
        if (!await _context.Quizzes.AnyAsync(q => q.Id == quizId, ct))
        {
            throw new NotFoundException("Quiz not found.");
        }

        // Materialize first: Guid.ToString() inside an EF projection becomes
        // SQL CONVERT(), which returns UPPERCASE strings.
        var rows = await _context.QuizQuestions
            .Where(qq => qq.QuizId == quizId)
            .OrderBy(qq => qq.DisplayOrder)
            .Select(qq => new
            {
                QuizQuestionId = qq.Id,
                QuestionId = qq.Question.Id,
                qq.Question.Content,
                qq.Question.QuestionType,
                qq.Question.IsActive,
                qq.DisplayOrder
            })
            .ToListAsync(ct);

        return rows.Select(r => new AssignedQuestionViewModel
        {
            QuizQuestionId = r.QuizQuestionId.ToString(),
            Id = r.QuestionId.ToString(),
            Content = r.Content,
            QuestionType = r.QuestionType,
            IsActive = r.IsActive,
            DisplayOrder = r.DisplayOrder
        }).ToList();
    }

    public async Task AddQuestionToQuizAsync(QuizQuestionCreateViewModel model, CancellationToken ct = default)
    {
        var quizId = GuidParsing.Parse(model.QuizId, "quiz");
        var questionId = GuidParsing.Parse(model.QuestionId, "question");

        if (!await _context.Quizzes.AnyAsync(q => q.Id == quizId, ct))
        {
            throw new NotFoundException("Quiz not found.");
        }
        if (!await _context.Questions.AnyAsync(q => q.Id == questionId, ct))
        {
            throw new NotFoundException("Question not found.");
        }
        if (await _context.QuizQuestions.AnyAsync(qq => qq.QuizId == quizId && qq.QuestionId == questionId, ct))
        {
            throw new ConflictException("This question is already assigned to the quiz.");
        }

        var nextOrder = await _context.QuizQuestions
            .Where(qq => qq.QuizId == quizId)
            .Select(qq => (int?)qq.DisplayOrder)
            .MaxAsync(ct) ?? 0;

        _context.QuizQuestions.Add(new QuizQuestion { QuizId = quizId, QuestionId = questionId, DisplayOrder = nextOrder + 1 });
        await _context.SaveChangesAsync(ct);
    }

    public async Task RemoveQuestionFromQuizAsync(Guid quizId, Guid quizQuestionId, CancellationToken ct = default)
    {
        var assignment = await _context.QuizQuestions
                             .FirstOrDefaultAsync(qq => qq.Id == quizQuestionId && qq.QuizId == quizId, ct)
                         ?? throw new NotFoundException("Question assignment not found.");

        _context.QuizQuestions.Remove(assignment);
        await _context.SaveChangesAsync(ct);
    }

    private int PageSize() => _configuration.GetValue("Paging:DefaultPageSize", 10);
    private int MaxPageSize() => _configuration.GetValue("Paging:MaxPageSize", 100);

    private static QuizViewModel Map(Quiz quiz) => new()
    {
        Id = quiz.Id.ToString(),
        Title = quiz.Title,
        Description = quiz.Description,
        Duration = quiz.Duration,
        IsActive = quiz.IsActive,
        ThumbnailUrl = quiz.ThumbnailUrl
    };
}

public static class GuidParsing
{
    public static Guid Parse(string? value, string entity)
    {
        if (!Guid.TryParse(value, out var id))
        {
            throw new BadRequestException($"Invalid {entity} id.");
        }
        return id;
    }
}
