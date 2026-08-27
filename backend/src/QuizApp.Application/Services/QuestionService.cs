using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuizApp.Application.Common;
using QuizApp.Application.Common.Exceptions;
using QuizApp.Application.Common.Paging;
using QuizApp.Application.DTOs.Questions;
using QuizApp.Application.Interfaces;
using QuizApp.Domain.Entities;

namespace QuizApp.Application.Services;

public class QuestionService : IQuestionService
{
    private static readonly IReadOnlyDictionary<string, Expression<Func<Question, object?>>> Sortable =
        new Dictionary<string, Expression<Func<Question, object?>>>
        {
            ["content"] = q => q.Content,
            ["questiontype"] = q => q.QuestionType,
            ["isactive"] = q => q.IsActive,
            ["createdat"] = q => q.CreatedAt
        };

    private readonly IAppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IValidator<QuestionCreateViewModel>? _createValidator;
    private readonly IValidator<QuestionEditViewModel>? _editValidator;

    public QuestionService(
        IAppDbContext context,
        IConfiguration configuration,
        IValidator<QuestionCreateViewModel>? createValidator = null,
        IValidator<QuestionEditViewModel>? editValidator = null)
    {
        _context = context;
        _configuration = configuration;
        _createValidator = createValidator;
        _editValidator = editValidator;
    }

    public Task<PagedResult<QuestionViewModel>> GetPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        IQueryable<Question> questions = _context.Questions;
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            questions = questions.Where(q => q.Content.Contains(search));
        }

        return questions.ToPagedResultAsync(query, Sortable, "createdat", Map,
            _configuration.GetValue("Paging:DefaultPageSize", 10),
            _configuration.GetValue("Paging:MaxPageSize", 100), ct);
    }

    public async Task<QuestionViewModel> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var question = await _context.Questions.FindAsync(new object[] { id }, ct)
                       ?? throw new NotFoundException("Question not found.");
        return Map(question);
    }

    public async Task<QuestionViewModel> CreateAsync(QuestionCreateViewModel model, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(_createValidator, model, ct);

        var question = new Question
        {
            Content = model.Content.Trim(),
            QuestionType = model.QuestionType,
            IsActive = model.IsActive
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync(ct);
        return Map(question);
    }

    public async Task<QuestionViewModel> UpdateAsync(QuestionEditViewModel model, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(_editValidator, model, ct);

        var question = await _context.Questions.FindAsync(new object[] { GuidParsing.Parse(model.Id, "question") }, ct)
                       ?? throw new NotFoundException("Question not found.");

        question.Content = model.Content.Trim();
        question.QuestionType = model.QuestionType;
        question.IsActive = model.IsActive;

        await _context.SaveChangesAsync(ct);
        return Map(question);
    }

    public async Task<QuestionDeletePreview> GetDeletePreviewAsync(Guid id, CancellationToken ct = default)
    {
        if (!await _context.Questions.AnyAsync(q => q.Id == id, ct))
        {
            throw new NotFoundException("Question not found.");
        }

        var quizTitles = await _context.QuizQuestions
            .Where(qq => qq.QuestionId == id)
            .Select(qq => qq.Quiz.Title)
            .ToListAsync(ct);

        return new QuestionDeletePreview
        {
            IsAssignedToQuizzes = quizTitles.Count > 0,
            QuizTitles = quizTitles.ToArray(),
            Warning = quizTitles.Count > 0
                ? $"This question is assigned to {quizTitles.Count} quiz(es): {string.Join(", ", quizTitles)}. Deleting it removes it from those quizzes."
                : string.Empty
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var question = await _context.Questions
                           .Include(q => q.Answers)
                           .Include(q => q.QuizQuestions)
                           .FirstOrDefaultAsync(q => q.Id == id, ct)
                       ?? throw new NotFoundException("Question not found.");

        // Preserve attempt history: null out UserAnswer references (snapshot columns keep the content).
        var answerIds = question.Answers.Select(a => a.Id).ToList();
        var referencing = await _context.UserAnswers
            .Where(ua => ua.QuestionId == id || (ua.AnswerId != null && answerIds.Contains(ua.AnswerId.Value)))
            .ToListAsync(ct);
        foreach (var userAnswer in referencing)
        {
            if (userAnswer.QuestionId == id)
            {
                userAnswer.QuestionId = null;
            }
            if (userAnswer.AnswerId is not null && answerIds.Contains(userAnswer.AnswerId.Value))
            {
                userAnswer.AnswerId = null;
            }
        }

        _context.Questions.Remove(question); // cascades to its answers and quiz assignments
        await _context.SaveChangesAsync(ct);
    }

    private static QuestionViewModel Map(Question question) => new()
    {
        Id = question.Id.ToString(),
        Content = question.Content,
        QuestionType = question.QuestionType,
        IsActive = question.IsActive
    };
}
