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
        var user = await authorization.RequireUserAsync(cancellationToken);

        var quiz = await quizzes.GetByIdAsync(quizId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Quiz), quizId);

        if (!quiz.IsActive)
            throw new BusinessRuleException("QuizInactive", "This quiz is not available.");

        var now = clock.GetUtcNow()
            .UtcDateTime;

        var questions = quiz.QuizQuestions
            .Where(qq => qq.QuestionNavigation.IsActive)
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

        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            QuizId = quiz.Id,
            UserNavigation = user,
            QuizNavigation = quiz,
            StartedAt = now,
            ExpiresAt = now.AddMinutes(quiz.Duration)
        };

        attempts.Add(attempt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new QuizAttemptStartDto
        {
            AttemptId = attempt.Id,
            Quiz = new QuizForAttemptDto
            {
                QuizId = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                Duration = quiz.Duration,
                Image = quiz.Image, PassedScore = quiz.PassedScore,
                Questions = questions
            },
            StartedAt = attempt.StartedAt,
            ExpiresAt = attempt.ExpiresAt
        };
    }

    public async Task<QuizAttemptDetailDto> SubmitAsync(Guid quizId, SubmitQuizDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await authorization.RequireUserAsync(cancellationToken);

        ArgumentNullException.ThrowIfNull(request);
        await submitValidator.ValidateAndThrowAsync(request, cancellationToken);

        var attempt = await attempts.GetByIdAsync(request.AttemptId, cancellationToken)
                      ?? throw new NotFoundException(nameof(QuizAttempt), request.AttemptId);

        if (attempt.UserId != user.Id)
            throw new ForbiddenException("You cannot submit another user's attempt.");

        if (attempt.QuizId != quizId)
            throw new BusinessRuleException("AttemptQuizMismatch", "This attempt belongs to a different quiz.");

        if (attempt.SubmitAt is not null)
            throw new ConflictException(nameof(QuizAttempt), nameof(QuizAttempt.Id), attempt.Id.ToString());

        var now = clock.GetUtcNow().UtcDateTime;

        if (now >= attempt.ExpiresAt)
            throw new BusinessRuleException("AttemptExpired", "The time allowed for this attempt has expired.");

        var quiz = await quizzes.GetByIdAsync(quizId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Quiz), quizId);

        if (!quiz.IsActive)
            throw new BusinessRuleException("QuizInactive", "This quiz is not available.");

        var orderedQuestions = quiz.QuizQuestions
            .Where(qq => qq.QuestionNavigation.IsActive)
            .OrderBy(qq => qq.Order)
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

            if (submission.AnswerIds.Count > 0)
            {
                var validAnswerIds = question.Answers
                    .Where(a => a.IsActive)
                    .Select(a => a.Id)
                    .ToHashSet();

                var invalidId = submission.AnswerIds.FirstOrDefault(id => !validAnswerIds.Contains(id));
                if (invalidId != default)
                    throw new Quizapp.Domain.Exceptions.ValidationException(nameof(SubmitQuizDto.Answers),
                        $"Answer '{invalidId}' is not a valid active option for question '{question.Id}'.");
            }

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

        attempt.SubmitAt = now;
        attempt.Score = finalScore;

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
            QuizTitle = attempt.QuizNavigation.Title,
            Score = attempt.Score,
            SubmittedAt = attempt.SubmitAt!.Value
        });
    }

    private static QuizAttemptDetailDto MapToDetail(QuizAttempt attempt)
    {
        var quiz = attempt.QuizNavigation;

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

                var selectedAnswer = submission.AnswerIds.Count == 1
                    ? question.Answers.FirstOrDefault(answer =>
                        answer.Id == submission.AnswerIds[0] && answer.QuestionId == question.Id && answer.IsActive)
                    : null;

                if (selectedAnswer is null)
                    throw new Quizapp.Domain.Exceptions.ValidationException(nameof(SubmitQuizDto.Answers),
                        $"Question '{question.Id}' requires exactly one active answer belonging to that question.");

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
