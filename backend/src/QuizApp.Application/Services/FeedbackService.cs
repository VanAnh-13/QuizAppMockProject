using FluentValidation;
using Microsoft.Extensions.Logging;
using QuizApp.Application.Common;
using QuizApp.Application.DTOs.Feedback;
using QuizApp.Application.Interfaces;
using QuizApp.Domain.Entities;

namespace QuizApp.Application.Services;

/// <summary>Contact-form submissions: stored + logged, no SMTP (per scope).</summary>
public class FeedbackService : IFeedbackService
{
    private readonly IAppDbContext _context;
    private readonly ILogger<FeedbackService> _logger;
    private readonly IValidator<FeedbackCreateViewModel>? _validator;

    public FeedbackService(
        IAppDbContext context,
        ILogger<FeedbackService> logger,
        IValidator<FeedbackCreateViewModel>? validator = null)
    {
        _context = context;
        _logger = logger;
        _validator = validator;
    }

    public async Task SubmitAsync(FeedbackCreateViewModel model, CancellationToken ct = default)
    {
        await ValidationHelper.ValidateAsync(_validator, model, ct);

        _context.Feedbacks.Add(new Feedback
        {
            Name = model.Name.Trim(),
            Email = model.Email.Trim(),
            Subject = model.Subject?.Trim(),
            Message = model.Message.Trim()
        });

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Feedback received from {Name} <{Email}>: {Subject}", model.Name, model.Email, model.Subject ?? "(no subject)");
    }
}
