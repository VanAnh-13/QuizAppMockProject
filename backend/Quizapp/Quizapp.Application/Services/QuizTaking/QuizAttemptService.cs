using System.Text.Json;
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
    IValidator<SubmitQuizDto> submitValidator,
    IValidator<AttemptRevisionDto> revisionValidator) : IQuizAttemptService
{
    public async Task<QuizAttemptDetailDto> SubmitSavedAsync(Guid attemptId, AttemptRevisionDto request,
        CancellationToken cancellationToken = default)
    {
        var attempt = await GetOwnedAttemptAsync(attemptId, cancellationToken);
        await RequireRevisionAsync(attempt, request, cancellationToken);

        return await CompleteAsync(attempt, ReadSavedAnswers(attempt), cancellationToken);
    }

    public async Task<PagedResultDto<QuizAttemptSummaryDto>> GetInProgressAsync(int pageNumber, int pageSize,
        Guid? quizId = null, CancellationToken cancellationToken = default)
    {
        var user = await authorization.RequireUserAsync(cancellationToken);
        ServiceRules.ValidatePage(pageNumber, pageSize);
        var page = await attempts.GetInProgressAsync(user.Id, pageNumber, pageSize, quizId, cancellationToken);

        var now = clock.GetUtcNow().UtcDateTime;

        return ServiceRules.MapPage(page, attempt => new QuizAttemptSummaryDto
        {
            AttemptId = attempt.Id, 
            QuizId = attempt.QuizId, 
            QuizTitle = AttemptQuizSnapshot.Read(attempt).Title,
            StartedAt = attempt.StartedAt,
            ExpiresAt = attempt.ExpiresAt,
            PausedAt = attempt.PausedAt,
            ServerTime = now,
            Revision = attempt.Revision,
            RemainingSeconds = Math.Max(0, (attempt.ExpiresAt - (attempt.PausedAt ?? now)).TotalSeconds)
        });
    }

    public async Task<QuizAttemptProgressDto> PauseAsync(Guid attemptId, SaveQuizProgressDto request,
        CancellationToken cancellationToken = default)
    {
        var attempt = await GetOwnedAttemptAsync(attemptId, cancellationToken);

        var now = clock.GetUtcNow().UtcDateTime;

        await SaveDraftAsync(attempt, request, now, cancellationToken);
        attempt.PausedAt = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MapProgress(attempt, now);
    }

    public async Task<QuizAttemptProgressDto> ResumeAsync(Guid attemptId, AttemptRevisionDto request,
        CancellationToken cancellationToken = default)
    {
        var attempt = await GetOwnedAttemptAsync(attemptId, cancellationToken);
        await RequireRevisionAsync(attempt, request, cancellationToken);

        var now = clock.GetUtcNow().UtcDateTime;

        if (attempt.PausedAt is { } pausedAt)
        {
            attempt.ExpiresAt = now.Add(attempt.ExpiresAt - pausedAt);
            attempt.PausedAt = null;
            attempt.Revision++;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else if (now >= attempt.ExpiresAt)
            throw new BusinessRuleException("AttemptExpired", "The time allowed for this attempt has expired.");

        return MapProgress(attempt, now);
    }

    public async Task<QuizAttemptProgressDto> GetProgressAsync(Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        var attempt = await GetOwnedAttemptAsync(attemptId, cancellationToken);
        RequireUnsubmitted(attempt);

        return MapProgress(attempt, clock.GetUtcNow().UtcDateTime);
    }

    public async Task<QuizAttemptProgressDto> SaveProgressAsync(Guid attemptId, SaveQuizProgressDto request,
        CancellationToken cancellationToken = default)
    {
        var attempt = await GetOwnedAttemptAsync(attemptId, cancellationToken);

        var now = clock.GetUtcNow().UtcDateTime;

        await SaveDraftAsync(attempt, request, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MapProgress(attempt, now);
    }

    private async Task<QuizAttempt> GetOwnedAttemptAsync(Guid attemptId, CancellationToken cancellationToken)
    {
        var user = await authorization.RequireUserAsync(cancellationToken);

        var attempt = await attempts.GetByIdAsync(attemptId, cancellationToken)
                      ?? throw new NotFoundException(nameof(QuizAttempt), attemptId);

        return attempt.UserId != user.Id
            ? throw new ForbiddenException("You do not have access to this attempt.")
            : attempt;
    }

    private static void RequireUnsubmitted(QuizAttempt attempt)
    {
        if (attempt.SubmitAt is not null)
            throw new ConflictException(nameof(QuizAttempt), nameof(QuizAttempt.Id), attempt.Id.ToString());
    }

    private async Task RequireRevisionAsync(QuizAttempt attempt, AttemptRevisionDto request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireUnsubmitted(attempt);
        await revisionValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (request.Revision != attempt.Revision)
            throw new ConflictException(nameof(QuizAttempt), nameof(QuizAttempt.Revision), attempt.Revision.ToString());
    }

    private async Task SaveDraftAsync(QuizAttempt attempt, SaveQuizProgressDto request, DateTime now,
        CancellationToken cancellationToken)
    {
        await RequireRevisionAsync(attempt, request, cancellationToken);

        if (attempt.PausedAt is not null)
            throw new BusinessRuleException("AttemptPaused", "Resume this attempt before changing answers.");

        if (now >= attempt.ExpiresAt)
            throw new BusinessRuleException("AttemptExpired", "The time allowed for this attempt has expired.");

        await submitValidator.ValidateAndThrowAsync(new SubmitQuizDto
        {
            AttemptId = attempt.Id,
            Answers = request.Answers
        }, cancellationToken);

        ValidateAnswers(AttemptQuizSnapshot.Read(attempt), request.Answers);
        attempt.QuizSnapshotJson ??= AttemptQuizSnapshot.Capture(attempt.QuizNavigation);
        attempt.DraftAnswersJson = JsonSerializer.Serialize(request.Answers);
        attempt.LastSavedAt = now;
        attempt.Revision++;
    }

    private static void ValidateAnswers(Quiz quiz, IEnumerable<SubmitAnswerDto> answers)
    {
        var questions = quiz.QuizQuestions.Where(qq => qq.QuestionNavigation.IsActive)
            .ToDictionary(qq => qq.QuestionId, qq => qq.QuestionNavigation);

        foreach (var response in answers)
        {
            if (!questions.TryGetValue(response.QuestionId, out var question))
                throw new Quizapp.Domain.Exceptions.ValidationException(nameof(SubmitQuizDto.Answers),
                    "An answer refers to a question outside this attempt.");

            var usesOptions = question.QuestionType is QuestionType.SingleChoice
                or QuestionType.MultipleChoice or QuestionType.TrueFalse;

            if (usesOptions != (response.AnswerIds.Count > 0)
                || response.AnswerIds.Any(id => !question.Answers.Any(a => a.Id == id && a.IsActive)))
                throw new Quizapp.Domain.Exceptions.ValidationException(nameof(SubmitQuizDto.Answers),
                    "The response does not match this question's type or active answer options.");

            if (question.QuestionType is QuestionType.SingleChoice or QuestionType.TrueFalse
                && response.AnswerIds.Count != 1)
                throw new Quizapp.Domain.Exceptions.ValidationException(nameof(SubmitQuizDto.Answers),
                    $"Question '{question.Id}' requires exactly one active answer belonging to that question.");
        }
    }

    private static List<SubmitAnswerDto> ReadSavedAnswers(QuizAttempt attempt) =>
        JsonSerializer.Deserialize<List<SubmitAnswerDto>>(attempt.DraftAnswersJson)
        ?? throw new InvalidOperationException("The attempt's saved answers are invalid.");

    private static QuizAttemptProgressDto MapProgress(QuizAttempt attempt, DateTime now) => new()
    {
        AttemptId = attempt.Id,
        StartedAt = attempt.StartedAt,
        ExpiresAt = attempt.ExpiresAt,
        ServerTime = now,
        Revision = attempt.Revision,
        PausedAt = attempt.PausedAt,
        LastSavedAt = attempt.LastSavedAt,
        RemainingSeconds = Math.Max(0, (attempt.ExpiresAt - (attempt.PausedAt ?? now)).TotalSeconds),
        Answers = ReadSavedAnswers(attempt),
        Quiz = MapQuiz(AttemptQuizSnapshot.Read(attempt))
    };

    private static QuizForAttemptDto MapQuiz(Quiz quiz) => new()
    {
        QuizId = quiz.Id,
        Title = quiz.Title,
        Description = quiz.Description,
        Duration = quiz.Duration,
        Image = quiz.Image,
        PassedScore = quiz.PassedScore,
        Questions =
        [
            .. quiz.QuizQuestions.Where(qq => qq.QuestionNavigation.IsActive)
                .OrderBy(qq => qq.Order)
                .Select(qq => new QuestionForAttemptDto
                {
                    Id = qq.QuestionId,
                    Content = qq.QuestionNavigation.Content,
                    Image = qq.QuestionNavigation.Image,
                    Level = qq.QuestionNavigation.Level,
                    QuestionType = qq.QuestionNavigation.QuestionType,
                    Order = qq.Order,
                    Answers = qq.QuestionNavigation.QuestionType is QuestionType.SingleChoice
                        or QuestionType.MultipleChoice or QuestionType.TrueFalse
                        ? qq.QuestionNavigation.Answers.Where(a => a.IsActive)
                            .Select(a => new AnswerOptionDto { Id = a.Id, Text = a.Text })
                            .ToArray()
                        : []
                })
        ]
    };

    public async Task<QuizAttemptStartDto> StartAsync(Guid quizId, CancellationToken cancellationToken = default)
    {
        var user = await authorization.RequireUserAsync(cancellationToken);

        var quiz = await quizzes.GetByIdAsync(quizId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Quiz), quizId);

        if (!quiz.IsActive)
            throw new BusinessRuleException("QuizInactive", "This quiz is not available.");

        var now = clock.GetUtcNow()
            .UtcDateTime;

        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            QuizId = quiz.Id,
            UserNavigation = user,
            QuizNavigation = quiz,
            StartedAt = now,
            ExpiresAt = now.AddMinutes(quiz.Duration),
            QuizSnapshotJson = AttemptQuizSnapshot.Capture(quiz)
        };

        attempts.Add(attempt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new QuizAttemptStartDto
        {
            AttemptId = attempt.Id,
            Quiz = MapQuiz(quiz),
            StartedAt = attempt.StartedAt,
            ExpiresAt = attempt.ExpiresAt,
            ServerTime = now,
            Revision = attempt.Revision
        };
    }

    public async Task<QuizAttemptDetailDto> SubmitAsync(Guid quizId, SubmitQuizDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await submitValidator.ValidateAsync(request, options =>
        {
            options.IncludeProperties(nameof(SubmitQuizDto.AttemptId), nameof(SubmitQuizDto.Revision));
            options.ThrowOnFailures();
        }, cancellationToken);

        var attempt = await GetOwnedAttemptAsync(request.AttemptId, cancellationToken);

        if (attempt.QuizId != quizId)
            throw new BusinessRuleException("AttemptQuizMismatch", "This attempt belongs to a different quiz.");

        if (request.Revision.HasValue || attempt.Revision > 0)
            await RequireRevisionAsync(attempt, new AttemptRevisionDto { Revision = request.Revision },
                cancellationToken);

        return await CompleteAsync(attempt, request.Answers, cancellationToken);
    }

    private async Task<QuizAttemptDetailDto> CompleteAsync(QuizAttempt attempt, List<SubmitAnswerDto> answers,
        CancellationToken cancellationToken)
    {
        RequireUnsubmitted(attempt);

        if (attempt.PausedAt is not null)
            throw new BusinessRuleException("AttemptPaused", "Resume this attempt before submitting it.");

        var now = clock.GetUtcNow().UtcDateTime;

        var expired = now >= attempt.ExpiresAt;
        var submittedAt = expired ? attempt.ExpiresAt : now;
        var responses = expired ? ReadSavedAnswers(attempt) : answers;

        await submitValidator.ValidateAndThrowAsync(new SubmitQuizDto
        {
            AttemptId = attempt.Id,
            Answers = responses
        }, cancellationToken);

        var quiz = AttemptQuizSnapshot.Read(attempt);

        if (!quiz.IsActive)
            throw new BusinessRuleException("QuizInactive", "This quiz is not available.");

        var orderedQuestions = quiz.QuizQuestions
            .Where(qq => qq.QuestionNavigation.IsActive)
            .OrderBy(qq => qq.Order)
            .Select(qq => qq.QuestionNavigation)
            .ToArray();

        ValidateAnswers(quiz, responses);
        var answerMap = responses.ToDictionary(a => a.QuestionId);

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
                QuestionId = question.Id,
                QuestionContent = question.Content,
                QuestionType = question.QuestionType,
                Image = question.Image,
                Level = question.Level,
                SelectedAnswers = selectedAnswers,
                ResponseText = submission?.ResponseText
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

        attempt.SubmitAt = submittedAt;
        attempt.Score = finalScore;
        attempt.Revision++;
        attempt.DraftAnswersJson = "[]";

        foreach (var (qId, aId, text) in pendingAnswers)
            attempt.UserAnswers.Add(new UserAnswer
            {
                QuizAttemptId = attempt.Id,
                QuestionId = qId,
                AnswerId = aId,
                ResponseText = text
            });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new QuizAttemptDetailDto
        {
            Id = attempt.Id,
            QuizId = quiz.Id,
            QuizTitle = quiz.Title,
            Score = finalScore,
            SubmittedAt = submittedAt,
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

        return attempt.SubmitAt is null
            ? throw new BusinessRuleException("AttemptNotSubmitted", "This attempt has not been submitted.")
            : MapToDetail(attempt);
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
            QuizTitle = AttemptQuizSnapshot.Read(attempt)
                .Title,
            Score = attempt.Score,
            SubmittedAt = attempt.SubmitAt!.Value
        });
    }

    private static QuizAttemptDetailDto MapToDetail(QuizAttempt attempt)
    {
        var quiz = AttemptQuizSnapshot.Read(attempt);

        var orderedQuestions = quiz.QuizQuestions
            .Where(qq => qq.QuestionNavigation.IsActive)
            .OrderBy(qq => qq.Order)
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
            SubmittedAt = attempt.SubmitAt!.Value,
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
                if (submission is null)
                    return 0.0;

                var selectedAnswer = question.Answers.Single(answer => answer.Id == submission.AnswerIds[0]);

                return selectedAnswer.IsCorrect ? 1.0 : 0.0;
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