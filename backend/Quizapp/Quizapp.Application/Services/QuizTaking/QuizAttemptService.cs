using FluentValidation;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Common;
using Quizapp.Application.DTOs.QuizHistory;
using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Application.Services.Common;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Application.Services.QuizTaking;

public sealed class QuizAttemptService(
    IQuizRepository quizzes,
    IQuizAttemptRepository attempts,
    IUnitOfWork unitOfWork,
    ServiceAuthorization authorization,
    TimeProvider clock,
    IValidator<SubmitQuizDto> submitValidator) : IQuizAttemptService
{
    public async Task<QuizAttemptStartDto> StartAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireUserAsync(cancellationToken);

        var quiz = await quizzes.GetByIdAsync(quizId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Quiz), quizId);

        if (!quiz.IsActive)
            throw new BusinessRuleException("QuizInactive", "This quiz is not available.");

        var now = clock.GetUtcNow()
            .UtcDateTime;

        var questions = quiz.QuizQuestions
            .OrderBy(qq => qq.Order)
            .Select(qq => new QuestionForAttemptDto
            {
                Id = qq.QuestionNavigation.Id,
                Content = qq.QuestionNavigation.Content,
                Image = qq.QuestionNavigation.Image,
                Level = qq.QuestionNavigation.Level,
                QuestionType = qq.QuestionNavigation.QuestionType,
                Order = qq.Order,
                Answers =
                [
                    .. qq.QuestionNavigation.Answers
                        .Where(a => a.IsActive)
                        .Select(a => new AnswerOptionDto { Id = a.Id, Text = a.Text })
                ]
            })
            .ToArray();

        return new QuizAttemptStartDto
        {
            AttemptId = Guid.NewGuid(),
            Quiz = new QuizForAttemptDto
            {
                QuizId = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                Duration = quiz.Duration,
                Image = quiz.Image, PassedScore = quiz.PassedScore,
                Questions = questions
            },
            StartedAt = now,
            ExpiresAt = now.AddMinutes(quiz.Duration)
        };
    }

    public async Task<QuizAttemptDetailDto> SubmitAsync(Guid quizId, SubmitQuizDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await authorization.RequireUserAsync(cancellationToken);

        ArgumentNullException.ThrowIfNull(request);
        await submitValidator.ValidateAndThrowAsync(request, cancellationToken);

        var quiz = await quizzes.GetByIdAsync(quizId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Quiz), quizId);

        if (!quiz.IsActive)
            throw new BusinessRuleException("QuizInactive", "This quiz is not available.");

        var orderedQuestions = quiz.QuizQuestions.OrderBy(qq => qq.Order)
            .Select(qq => qq.QuestionNavigation)
            .ToArray();

        var answerMap = request.Answers.ToDictionary(a => a.QuestionId);

        var totalPoints = 0.0;
        var userAnswerResults = new List<UserAnswerResultDto>();
        var pendingAnswers = new List<(Guid QuestionId, Guid? AnswerId, string? ResponseText)>();

        foreach (var question in orderedQuestions)
        {
            answerMap.TryGetValue(question.Id, out var submission);

            var points = Score(question, submission);
            totalPoints += points;

            var selectedAnswers = question.Answers
                .Where(a => submission?.AnswerIds.Contains(a.Id) == true)
                .Select(a => new AnswerOptionDto { Id = a.Id, Text = a.Text })
                .ToArray();

            userAnswerResults.Add(new UserAnswerResultDto
            {
                QuestionId = question.Id, QuestionContent = question.Content,
                QuestionType = question.QuestionType, Image = question.Image, Level = question.Level,
                SelectedAnswers = selectedAnswers, ResponseText = submission?.ResponseText
            });

            if (submission is null) continue;

            pendingAnswers.AddRange(submission.AnswerIds
                .Select(answerId =>
                    ((Guid QuestionId, Guid? AnswerId, string? ResponseText))(question.Id, answerId, null))
            );

            if (submission.AnswerIds.Count == 0 && submission.ResponseText is not null)
                pendingAnswers.Add((question.Id, null, submission.ResponseText));
        }

        var finalScore = orderedQuestions.Length > 0
            ? Math.Round(totalPoints / orderedQuestions.Length * 100, 2)
            : 0;

        var now = clock.GetUtcNow()
            .UtcDateTime;

        var attemptId = Guid.NewGuid();

        var attempt = new QuizAttempt
        {
            Id = attemptId, UserId = user.Id, QuizId = quiz.Id,
            SubmitAt = now, Score = finalScore
        };

        foreach (var (qId, aId, text) in pendingAnswers)
            attempt.UserAnswers.Add(new UserAnswer
            {
                Id = Guid.NewGuid(),
                QuizAttemptId = attemptId,
                QuestionId = qId,
                AnswerId = aId,
                ResponseText = text
            });

        attempts.Add(attempt);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new QuizAttemptDetailDto
        {
            Id = attempt.Id,
            QuizId = quiz.Id,
            QuizTitle = quiz.Title,
            Score = finalScore,
            SubmittedAt = now,
            Answers = userAnswerResults
        };
    }

    public async Task<QuizAttemptDetailDto> GetResultAsync(Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        var user = await authorization.RequireUserAsync(cancellationToken);

        var attempt = await attempts.GetByIdAsync(attemptId, cancellationToken)
                      ?? throw new NotFoundException(nameof(QuizAttempt), attemptId);

        if (attempt.UserId != user.Id && !ServiceAuthorization.IsAdmin(user))
            throw new ForbiddenException("You do not have access to this attempt.");

        return MapToDetail(attempt);
    }

    public async Task<PagedResultDto<QuizAttemptDto>> GetHistoryAsync(int pageNumber, int pageSize, Guid? quizId = null,
        CancellationToken cancellationToken = default)
    {
        var user = await authorization.RequireUserAsync(cancellationToken);
        ServiceRules.ValidatePage(pageNumber, pageSize);

        var page = await attempts.GetHistoryAsync(user.Id, pageNumber, pageSize, quizId, cancellationToken);

        return ServiceRules.MapPage(page, attempt => new QuizAttemptDto
        {
            Id = attempt.Id,
            QuizId = attempt.QuizId,
            QuizTitle = attempt.QuizNavigation.Title,
            Score = attempt.Score,
            SubmittedAt = attempt.SubmitAt
        });
    }

    private static QuizAttemptDetailDto MapToDetail(QuizAttempt attempt)
    {
        var quiz = attempt.QuizNavigation;

        var orderedQuestions = quiz.QuizQuestions.OrderBy(qq => qq.Order)
            .Select(qq => qq.QuestionNavigation)
            .ToArray();

        var answersByQuestion = attempt.UserAnswers.GroupBy(ua => ua.QuestionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = orderedQuestions.Select(question =>
            {
                answersByQuestion.TryGetValue(question.Id, out var userAnswers);

                var selectedAnswers = question.Answers
                    .Where(a => userAnswers?.Any(ua => ua.AnswerId == a.Id) == true)
                    .Select(a => new AnswerOptionDto { Id = a.Id, Text = a.Text })
                    .ToArray();

                var responseText = userAnswers?.FirstOrDefault(ua => ua.ResponseText is not null)
                    ?.ResponseText;

                return new UserAnswerResultDto
                {
                    QuestionId = question.Id,
                    QuestionContent = question.Content,
                    QuestionType = question.QuestionType,
                    Image = question.Image,
                    Level = question.Level,
                    SelectedAnswers = selectedAnswers,
                    ResponseText = responseText
                };
            })
            .ToArray();

        return new QuizAttemptDetailDto
        {
            Id = attempt.Id,
            QuizId = attempt.QuizId,
            QuizTitle = quiz.Title,
            Score = attempt.Score,
            SubmittedAt = attempt.SubmitAt,
            Answers = results
        };
    }

    private static double Score(Question question, SubmitAnswerDto? submission)
    {
        switch (question.QuestionType)
        {
            case QuestionType.SingleChoice:

            case QuestionType.TrueFalse:
            {
                var correctId = question.Answers.FirstOrDefault(a => a is { IsCorrect: true, IsActive: true })
                    ?.Id;

                return correctId.HasValue && submission?.AnswerIds.Contains(correctId.Value) == true ? 1.0 : 0.0;
            }

            case QuestionType.MultipleChoice:
            {
                var correctIds = question.Answers.Where(a => a is { IsCorrect: true, IsActive: true })
                    .Select(a => a.Id)
                    .ToHashSet();

                if (correctIds.Count == 0) return 0.0;

                var selected = submission?.AnswerIds.ToHashSet() ?? [];
                var correctHits = selected.Count(correctIds.Contains);
                var wrongHits = selected.Count(id => !correctIds.Contains(id));

                return Math.Max(0.0, (double)(correctHits - wrongHits) / correctIds.Count);
            }

            case QuestionType.FillInTheBlanks:

            case QuestionType.ShortAnswer:

            case QuestionType.LongAnswer:
            {
                if (string.IsNullOrWhiteSpace(submission?.ResponseText)) return 0.0;

                var response = submission.ResponseText.Trim();

                return question.Answers
                    .Where(a => a is { IsCorrect: true, IsActive: true })
                    .Any(a => string.Equals(a.Text.Trim(), response, StringComparison.OrdinalIgnoreCase))
                    ? 1.0
                    : 0.0;
            }

            default:
                return 0.0;
        }
    }
}