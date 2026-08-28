using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuizApp.Application.Common.Exceptions;
using QuizApp.Application.DTOs.Attempts;
using QuizApp.Application.DTOs.QuizCodes;
using QuizApp.Application.Interfaces;
using QuizApp.Domain.Entities;
using QuizApp.Domain.Enums;

namespace QuizApp.Application.Services;

/// <summary>
/// Implements the quiz attempt lifecycle:
/// Prepared → InProgress → Submitted | Expired
///
/// All timing is server-side. isCorrect never leaves the server during an active attempt.
/// </summary>
public class QuizAttemptService : IQuizAttemptService
{
    private readonly IAppDbContext _context;
    private readonly IQuizCodeService _codeService;
    private readonly IScoringService _scoringService;
    private readonly UserManager<ApplicationUser> _userManager;

    public QuizAttemptService(
        IAppDbContext context,
        IQuizCodeService codeService,
        IScoringService scoringService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _codeService = codeService;
        _scoringService = scoringService;
        _userManager = userManager;
    }

    // ---- Prepare -------------------------------------------------------

    public async Task<QuizPrepareInfoViewModel> PrepareAsync(
        PrepareQuizViewModel model,
        Guid callerUserId,
        CancellationToken ct = default)
    {
        await _codeService.ValidateAsync(model.QuizCode, callerUserId, ct);

        var quizCode = await _context.QuizCodes
            .FirstOrDefaultAsync(qc => qc.Code == model.QuizCode, ct)
            ?? throw new NotFoundException("Quiz code not found.");

        var quiz = await _context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == quizCode.QuizId, ct)
            ?? throw new NotFoundException("Quiz not found.");

        var user = await _userManager.FindByIdAsync(callerUserId.ToString())
            ?? throw new NotFoundException("User not found.");

        // Create the attempt in the Prepared state.
        var attempt = new QuizAttempt
        {
            QuizId = quiz.Id,
            UserId = callerUserId,
            QuizCode = model.QuizCode,
            Status = QuizAttemptStatus.Prepared
        };
        _context.QuizAttempts.Add(attempt);

        // Mark the code as used so it cannot start another attempt.
        quizCode.IsUsed = true;

        await _context.SaveChangesAsync(ct);

        return new QuizPrepareInfoViewModel
        {
            Id = quiz.Id.ToString(),
            Title = quiz.Title,
            Description = quiz.Description,
            Duration = quiz.Duration,
            ThumbnailUrl = quiz.ThumbnailUrl,
            QuizCode = model.QuizCode,
            User = new QuizUserSnapshot
            {
                Id = user.Id.ToString(),
                DisplayName = user.DisplayName,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty
            }
        };
    }

    // ---- Take ----------------------------------------------------------

    public async Task<QuizForTestViewModel> TakeAsync(
        TakeQuizViewModel model,
        Guid callerUserId,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(model.QuizId, out var quizId))
            throw new BadRequestException("Invalid quiz id.");

        // Find the most recent Prepared attempt for (user, quiz, code).
        var attempt = await _context.QuizAttempts
            .FirstOrDefaultAsync(a =>
                a.UserId == callerUserId &&
                a.QuizId == quizId &&
                a.QuizCode == model.QuizCode &&
                a.Status == QuizAttemptStatus.Prepared, ct)
            ?? throw new NotFoundException("No prepared attempt found for this quiz code. Please start from the prepare page.");

        var quiz = await _context.Quizzes
            .Include(q => q.QuizQuestions.OrderBy(qq => qq.DisplayOrder))
                .ThenInclude(qq => qq.Question)
                    .ThenInclude(q => q.Answers.Where(a => a.IsActive))
            .FirstOrDefaultAsync(q => q.Id == quizId, ct)
            ?? throw new NotFoundException("Quiz not found.");

        // Record start time server-side and compute deadline.
        var now = DateTimeOffset.UtcNow;
        attempt.StartTime = now;
        attempt.Deadline = now.AddMinutes(quiz.Duration);
        attempt.Status = QuizAttemptStatus.InProgress;

        await _context.SaveChangesAsync(ct);

        // Project without isCorrect — this is the spec's critical security requirement.
        var questions = quiz.QuizQuestions
            .Where(qq => qq.Question != null)
            .Select(qq => new QuestionForTestViewModel
            {
                Id = qq.Question.Id.ToString(),
                Content = qq.Question.Content,
                QuestionType = qq.Question.QuestionType,
                Answers = qq.Question.Answers
                    .Select(a => new AnswerForTestViewModel
                    {
                        Id = a.Id.ToString(),
                        Content = a.Content
                        // isCorrect deliberately omitted
                    }).ToList()
            }).ToList();

        return new QuizForTestViewModel
        {
            Id = quiz.Id.ToString(),
            Title = quiz.Title,
            Description = quiz.Description,
            Duration = quiz.Duration,
            QuizCode = model.QuizCode,
            StartTime = attempt.StartTime.Value.ToString("o"),
            EndTime = attempt.Deadline.Value.ToString("o"),
            Questions = questions
        };
    }

    // ---- Submit --------------------------------------------------------

    public async Task<QuizResultViewModel> SubmitAsync(
        QuizSubmissionViewModel model,
        Guid callerUserId,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(model.QuizId, out var quizId))
            throw new BadRequestException("Invalid quiz id.");

        // Check for an already-completed attempt first (repeat-submit guard → 409).
        var completedAttempt = await _context.QuizAttempts
            .FirstOrDefaultAsync(a =>
                a.UserId == callerUserId &&
                a.QuizId == quizId &&
                a.QuizCode == model.QuizCode &&
                (a.Status == QuizAttemptStatus.Submitted || a.Status == QuizAttemptStatus.Expired), ct);

        if (completedAttempt != null)
            throw new ConflictException("This attempt has already been submitted.",
                new Dictionary<string, string[]> { ["attempt"] = ["Already submitted."] });

        // Find the active attempt.
        var attempt = await _context.QuizAttempts
            .FirstOrDefaultAsync(a =>
                a.UserId == callerUserId &&
                a.QuizId == quizId &&
                a.QuizCode == model.QuizCode &&
                (a.Status == QuizAttemptStatus.InProgress || a.Status == QuizAttemptStatus.Prepared), ct)
            ?? throw new NotFoundException("No active attempt found for this quiz code.");

        var now = DateTimeOffset.UtcNow;
        var isExpired = attempt.Deadline.HasValue && now > attempt.Deadline.Value;

        // Load quiz questions with their answers (including isCorrect for server-side grading).
        var quiz = await _context.Quizzes
            .Include(q => q.QuizQuestions.OrderBy(qq => qq.DisplayOrder))
                .ThenInclude(qq => qq.Question)
                    .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == quizId, ct)
            ?? throw new NotFoundException("Quiz not found.");

        // Build scoring input.
        var questionsForScoring = quiz.QuizQuestions
            .Where(qq => qq.Question != null)
            .Select(qq => new QuestionForScoring
            {
                QuestionId = qq.Question.Id,
                QuestionContent = qq.Question.Content,
                QuestionType = qq.Question.QuestionType,
                Answers = qq.Question.Answers.Select(a => new AnswerForScoring
                {
                    AnswerId = a.Id,
                    Content = a.Content,
                    IsCorrect = a.IsCorrect
                }).ToList()
            }).ToList();

        var scoring = _scoringService.Score(questionsForScoring, model.Answers);

        // Persist UserAnswer rows with attempt id set.
        foreach (var ua in scoring.UserAnswers)
        {
            ua.QuizAttemptId = attempt.Id;
            _context.UserAnswers.Add(ua);
        }

        // Update attempt status and scores.
        attempt.Status = isExpired ? QuizAttemptStatus.Expired : QuizAttemptStatus.Submitted;
        attempt.SubmittedAt = now;
        attempt.Score = scoring.Score;
        attempt.CorrectCount = scoring.CorrectCount;
        attempt.TotalQuestions = scoring.GradableCount;

        await _context.SaveChangesAsync(ct);

        return new QuizResultViewModel
        {
            AttemptId = attempt.Id.ToString(),
            QuizTitle = quiz.Title,
            Score = scoring.Score,
            CorrectCount = scoring.CorrectCount,
            TotalQuestions = scoring.GradableCount,
            SubmittedAt = attempt.SubmittedAt.Value.ToString("o")
        };
    }

    // ---- History -------------------------------------------------------

    public async Task<IReadOnlyList<AttemptSummaryViewModel>> GetMyAttemptsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var rows = await _context.QuizAttempts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.SubmittedAt ?? a.StartTime ?? DateTimeOffset.MinValue)
            .Select(a => new
            {
                a.Id,
                a.QuizId,
                QuizTitle = a.Quiz.Title,
                a.Score,
                a.CorrectCount,
                a.TotalQuestions,
                a.Status,
                a.SubmittedAt,
                a.StartTime
            })
            .ToListAsync(ct);

        return rows.Select(r => new AttemptSummaryViewModel
        {
            Id = r.Id.ToString(),
            QuizId = r.QuizId.ToString(),
            QuizTitle = r.QuizTitle,
            Score = r.Score,
            CorrectCount = r.CorrectCount,
            TotalQuestions = r.TotalQuestions,
            Status = r.Status.ToString(),
            SubmittedAt = r.SubmittedAt?.ToString("o"),
            StartTime = r.StartTime?.ToString("o")
        }).ToList();
    }

    // ---- Detail --------------------------------------------------------

    public async Task<AttemptDetailViewModel> GetAttemptDetailAsync(
        Guid attemptId,
        Guid callerUserId,
        bool isManager,
        CancellationToken ct = default)
    {
        var attempt = await _context.QuizAttempts
            .Include(a => a.UserAnswers)
            .Include(a => a.Quiz)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct)
            ?? throw new NotFoundException("Attempt not found.");

        // Authorization: only the owner or a manager can view the detail.
        if (!isManager && attempt.UserId != callerUserId)
            throw new ForbiddenException("You do not have permission to view this attempt.");

        var answers = attempt.UserAnswers
            .Select(ua => new AttemptAnswerViewModel
            {
                Id = ua.Id.ToString(),
                QuestionContent = ua.QuestionContent,
                QuestionType = ua.QuestionType,
                AnswerContent = ua.AnswerContent ?? ua.TextAnswer,
                CorrectAnswerContent = ua.CorrectAnswerContent,
                IsCorrect = ua.IsCorrect,
                NeedsReview = ua.NeedsReview
            }).ToList();

        return new AttemptDetailViewModel
        {
            Id = attempt.Id.ToString(),
            QuizId = attempt.QuizId.ToString(),
            QuizTitle = attempt.Quiz.Title,
            Score = attempt.Score,
            CorrectCount = attempt.CorrectCount,
            TotalQuestions = attempt.TotalQuestions,
            Status = attempt.Status.ToString(),
            SubmittedAt = attempt.SubmittedAt?.ToString("o"),
            StartTime = attempt.StartTime?.ToString("o"),
            Answers = answers
        };
    }
}
