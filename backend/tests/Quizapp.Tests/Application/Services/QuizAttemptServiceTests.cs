using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Application.Services.QuizTaking;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;
using Quizapp.Domain.Exceptions;
using Quizapp.Tests.Data;
using Quizapp.Infrastructure.Persistence;

namespace Quizapp.Tests.Application.Services;

public class QuizAttemptServiceTests
{
    [Fact]
    public async Task Start_persists_the_returned_attempt_for_the_current_user()
    {
        using var context = new ServiceTestContext();
        var quiz = CreateQuiz(context);
        var service = context.Get<IQuizAttemptService>();

        var start = await service.StartAsync(quiz.Id);

        var saved = await context.Get<IQuizAttemptRepository>().GetByIdAsync(start.AttemptId, CancellationToken.None);
        Assert.NotNull(saved);
        Assert.Equal(context.CurrentUser.UserId, saved.UserId);
        Assert.Equal(quiz.Id, saved.QuizId);
        Assert.Empty((await service.GetHistoryAsync(1, 10)).Items);
        Assert.Equal(start.StartedAt, saved.StartedAt);
        Assert.Equal(start.ExpiresAt, saved.ExpiresAt);
        Assert.Null(saved.SubmitAt);
    }

    [Fact]
    public async Task Submit_completes_the_started_attempt_and_returns_the_same_id()
    {
        using var context = new ServiceTestContext();
        var quiz = CreateQuiz(context);
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);
        var question = quiz.QuizQuestions.Single().QuestionNavigation;
        var persisted = await context.Get<IQuizAttemptRepository>().GetByIdAsync(start.AttemptId, CancellationToken.None);
        await using var db = new QuizAppDbContext(new DbContextOptionsBuilder<QuizAppDbContext>().UseSqlServer().Options);
        db.Attach(persisted!);

        var result = await service.SubmitAsync(quiz.Id, new SubmitQuizDto
        {
            AttemptId = start.AttemptId,
            Answers = [new SubmitAnswerDto { QuestionId = question.Id, AnswerIds = [question.Answers.Single().Id] }]
        });

        Assert.Equal(start.AttemptId, result.Id);
        Assert.Equal(100, result.Score);
        var saved = await context.Get<IQuizAttemptRepository>().GetByIdAsync(start.AttemptId, CancellationToken.None);
        Assert.Equal(result.SubmittedAt, saved!.SubmitAt);
        Assert.Equal(start.AttemptId, Assert.Single(saved.UserAnswers).QuizAttemptId);
        Assert.Equal(start.AttemptId, Assert.Single((await service.GetHistoryAsync(1, 10)).Items).Id);
        Assert.Equal(100, (await service.GetResultAsync(start.AttemptId)).Score);
        db.ChangeTracker.DetectChanges();
        Assert.Equal(EntityState.Added, Assert.Single(db.ChangeTracker.Entries<UserAnswer>()).State);
    }

    [Fact]
    public async Task Submitted_attempt_cannot_be_submitted_again()
    {
        using var context = new ServiceTestContext();
        var quiz = CreateQuiz(context);
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);
        var request = new SubmitQuizDto { AttemptId = start.AttemptId };
        var result = await service.SubmitAsync(quiz.Id, request);

        await Assert.ThrowsAsync<ConflictException>(() => service.SubmitAsync(quiz.Id, request));

        Assert.Equal(result.SubmittedAt, (await service.GetResultAsync(start.AttemptId)).SubmittedAt);
        Assert.Single((await service.GetHistoryAsync(1, 10)).Items);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Another_user_cannot_submit_an_attempt_even_if_they_are_an_admin(bool isAdmin)
    {
        using var context = new ServiceTestContext();
        var quiz = CreateQuiz(context);
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);
        var otherUser = TestEntities.User();
        otherUser.IsActive = true;
        if (isAdmin)
            otherUser.Roles.Add(context.Users.Rows.Single().Roles.Single());
        context.Users.Add(otherUser);
        context.CurrentUser.UserId = otherUser.Id;

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.SubmitAsync(quiz.Id, new SubmitQuizDto { AttemptId = start.AttemptId }));

        var saved = await context.Get<IQuizAttemptRepository>().GetByIdAsync(start.AttemptId, CancellationToken.None);
        Assert.Null(saved!.SubmitAt);
    }

    [Fact]
    public async Task Attempt_must_exist_and_belong_to_the_quiz_in_the_route()
    {
        using var context = new ServiceTestContext();
        var quiz = CreateQuiz(context);
        var otherQuiz = CreateQuiz(context);
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.SubmitAsync(quiz.Id, new SubmitQuizDto { AttemptId = Guid.NewGuid() }));
        var mismatch = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.SubmitAsync(otherQuiz.Id, new SubmitQuizDto { AttemptId = start.AttemptId }));
        Assert.Equal("AttemptQuizMismatch", mismatch.RuleName);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public async Task Submit_uses_the_persisted_deadline_including_the_expiry_boundary(int secondsFromExpiry, bool allowed)
    {
        using var context = new ServiceTestContext();
        var clock = new AttemptClock();
        context.Services.AddSingleton<TimeProvider>(clock);
        var quiz = CreateQuiz(context);
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);
        quiz.Duration *= 2;
        clock.Now = new DateTimeOffset(start.ExpiresAt.AddSeconds(secondsFromExpiry));
        var request = new SubmitQuizDto { AttemptId = start.AttemptId };

        if (allowed)
        {
            var result = await service.SubmitAsync(quiz.Id, request);
            Assert.Equal(start.AttemptId, result.Id);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.SubmitAsync(quiz.Id, request));
            Assert.Equal("AttemptExpired", exception.RuleName);
            var saved = await context.Get<IQuizAttemptRepository>().GetByIdAsync(start.AttemptId, CancellationToken.None);
            Assert.Null(saved!.SubmitAt);
        }
    }

    [Fact]
    public async Task An_unsubmitted_attempt_has_no_result()
    {
        using var context = new ServiceTestContext();
        var quiz = CreateQuiz(context);
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetResultAsync(start.AttemptId));

        Assert.Equal("AttemptNotSubmitted", exception.RuleName);
    }

    [Fact]
    public async Task Start_excludes_inactive_questions_from_the_attempt()
    {
        using var context = new ServiceTestContext();
        var quiz = CreateQuiz(context);
        var inactiveQuestion = TestEntities.Question(QuestionType.SingleChoice);
        inactiveQuestion.IsActive = false;
        inactiveQuestion.Answers.Add(new Answer
        {
            Id = Guid.NewGuid(), QuestionId = inactiveQuestion.Id, QuestionNavigation = inactiveQuestion,
            Text = "Option", IsCorrect = true, IsActive = true
        });
        quiz.QuizQuestions.Add(new QuizQuestion
        {
            Id = Guid.NewGuid(), QuizId = quiz.Id, QuestionId = inactiveQuestion.Id,
            QuizNavigation = quiz, QuestionNavigation = inactiveQuestion
        });
        var service = context.Get<IQuizAttemptService>();

        var start = await service.StartAsync(quiz.Id);

        Assert.Single(start.Quiz.Questions);
        Assert.DoesNotContain(start.Quiz.Questions, q => q.Id == inactiveQuestion.Id);
    }

    [Fact]
    public async Task Submit_excludes_inactive_questions_from_scoring_and_denominator()
    {
        using var context = new ServiceTestContext();
        var quiz = CreateQuiz(context);
        // Add a second, inactive question — without the filter it would lower the score from 100 to 50.
        var inactiveQuestion = TestEntities.Question(QuestionType.SingleChoice);
        inactiveQuestion.IsActive = false;
        inactiveQuestion.Answers.Add(new Answer
        {
            Id = Guid.NewGuid(), QuestionId = inactiveQuestion.Id, QuestionNavigation = inactiveQuestion,
            Text = "Option", IsCorrect = true, IsActive = true
        });
        quiz.QuizQuestions.Add(new QuizQuestion
        {
            Id = Guid.NewGuid(), QuizId = quiz.Id, QuestionId = inactiveQuestion.Id,
            QuizNavigation = quiz, QuestionNavigation = inactiveQuestion
        });
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);
        var activeQuestion = quiz.QuizQuestions.First(qq => qq.QuestionNavigation.IsActive).QuestionNavigation;

        var result = await service.SubmitAsync(quiz.Id, new SubmitQuizDto
        {
            AttemptId = start.AttemptId,
            Answers =
            [
                new SubmitAnswerDto
                {
                    QuestionId = activeQuestion.Id,
                    AnswerIds = [activeQuestion.Answers.Single(a => a.IsCorrect).Id]
                }
            ]
        });

        Assert.Equal(100, result.Score);
        Assert.DoesNotContain(result.Answers, a => a.QuestionId == inactiveQuestion.Id);
    }

    [Fact]
    public async Task GetResult_excludes_inactive_questions_from_the_detail_view()
    {
        using var context = new ServiceTestContext();
        var quiz = CreateQuiz(context);
        var inactiveQuestion = TestEntities.Question(QuestionType.SingleChoice);
        inactiveQuestion.IsActive = false;
        inactiveQuestion.Answers.Add(new Answer
        {
            Id = Guid.NewGuid(), QuestionId = inactiveQuestion.Id, QuestionNavigation = inactiveQuestion,
            Text = "Option", IsCorrect = true, IsActive = true
        });
        quiz.QuizQuestions.Add(new QuizQuestion
        {
            Id = Guid.NewGuid(), QuizId = quiz.Id, QuestionId = inactiveQuestion.Id,
            QuizNavigation = quiz, QuestionNavigation = inactiveQuestion
        });
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);
        await service.SubmitAsync(quiz.Id, new SubmitQuizDto { AttemptId = start.AttemptId });

        var detail = await service.GetResultAsync(start.AttemptId);

        Assert.DoesNotContain(detail.Answers, a => a.QuestionId == inactiveQuestion.Id);
    }

    private sealed class AttemptClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static Quiz CreateQuiz(ServiceTestContext context)
    {
        var quiz = TestEntities.Quiz();
        quiz.IsActive = true;
        var question = TestEntities.Question(QuestionType.SingleChoice);
        question.IsActive = true;
        var answer = new Answer
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, QuestionNavigation = question,
            Text = "Correct option", IsCorrect = true, IsActive = true
        };
        question.Answers.Add(answer);
        quiz.QuizQuestions.Add(new QuizQuestion
        {
            Id = Guid.NewGuid(), QuizId = quiz.Id, QuestionId = question.Id,
            QuizNavigation = quiz, QuestionNavigation = question
        });
        context.Quizzes.Add(quiz);
        return quiz;
    }
}
