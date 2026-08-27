using FluentValidation;
using Microsoft.EntityFrameworkCore;
using QuizApp.Application.Common;
using QuizApp.Application.Common.Exceptions;
using QuizApp.Application.DTOs.Answers;
using QuizApp.Application.Interfaces;
using QuizApp.Domain.Entities;

namespace QuizApp.Application.Services;

public class AnswerService : IAnswerService
{
    private readonly IAppDbContext _context;
    private readonly IValidator<AnswerCreateViewModel>? _createValidator;
    private readonly IValidator<AnswerEditViewModel>? _editValidator;

    public AnswerService(
        IAppDbContext context,
        IValidator<AnswerCreateViewModel>? createValidator = null,
        IValidator<AnswerEditViewModel>? editValidator = null)
    {
        _context = context;
        _createValidator = createValidator;
        _editValidator = editValidator;
    }

    public async Task<IReadOnlyList<AnswerViewModel>> GetAnswersByQuestionIdAsync(Guid questionId, CancellationToken ct = default)
    {
        if (!await _context.Questions.AnyAsync(q => q.Id == questionId, ct))
        {
            throw new NotFoundException("Question not found.");
        }

        var answers = await _context.Answers
            .Where(a => a.QuestionId == questionId)
            .OrderBy(a => a.Content)
            .ToListAsync(ct);

        return answers.Select(Map).ToList();
    }

    public async Task<AnswerViewModel> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var answer = await _context.Answers.FindAsync(new object[] { id }, ct)
                     ?? throw new NotFoundException("Answer not found.");
        return Map(answer);
    }

    public async Task<AnswerViewModel> CreateAsync(AnswerCreateViewModel model, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(_createValidator, model, ct);

        var questionId = GuidParsing.Parse(model.QuestionId, "question");
        if (!await _context.Questions.AnyAsync(q => q.Id == questionId, ct))
        {
            throw new NotFoundException("Question not found.");
        }

        var answer = new Answer
        {
            Content = model.Content.Trim(),
            IsCorrect = model.IsCorrect,
            IsActive = model.IsActive,
            QuestionId = questionId
        };

        _context.Answers.Add(answer);
        await _context.SaveChangesAsync(ct);
        return Map(answer);
    }

    public async Task<AnswerViewModel> UpdateAsync(AnswerEditViewModel model, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(_editValidator, model, ct);

        var answer = await _context.Answers.FindAsync(new object[] { GuidParsing.Parse(model.Id, "answer") }, ct)
                     ?? throw new NotFoundException("Answer not found.");

        answer.Content = model.Content.Trim();
        answer.IsCorrect = model.IsCorrect;
        answer.IsActive = model.IsActive;

        await _context.SaveChangesAsync(ct);
        return Map(answer);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var answer = await _context.Answers.FindAsync(new object[] { id }, ct)
                     ?? throw new NotFoundException("Answer not found.");

        // Preserve attempt history: null out UserAnswer references first (snapshot columns keep the content).
        var referencing = await _context.UserAnswers.Where(ua => ua.AnswerId == id).ToListAsync(ct);
        foreach (var userAnswer in referencing)
        {
            userAnswer.AnswerId = null;
        }

        _context.Answers.Remove(answer);
        await _context.SaveChangesAsync(ct);
    }

    private static AnswerViewModel Map(Answer answer) => new()
    {
        Id = answer.Id.ToString(),
        Content = answer.Content,
        IsCorrect = answer.IsCorrect,
        IsActive = answer.IsActive,
        QuestionId = answer.QuestionId.ToString()
    };
}
