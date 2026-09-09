using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Application.Services.QuizTaking;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;
using Quizapp.Domain.Exceptions;
using Quizapp.Infrastructure.Persistence.Repositories;
using Quizapp.Tests.Application.Services;

namespace Quizapp.Tests.Data;

public class QuizAttemptPersistenceTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    [SqlServerFact]
    public async Task A_paused_draft_survives_reloading_and_completes_from_its_original_quiz_snapshot()
    {
        await using var db = database.CreateContext();
        var quiz = TestEntities.Quiz();
        quiz.IsActive = true;
        var user = TestEntities.User();
        user.IsActive = true;
        var question = TestEntities.Question(QuestionType.ShortAnswer);
        question.IsActive = true;
        question.Answers.Add(new Answer
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, Text = "Original", IsActive = true, IsCorrect = true
        });
        quiz.Questions.Add(question);
        db.AddRange(quiz, user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        using var services = new ServiceTestContext();
        services.CurrentUser.UserId = user.Id;
        services.Services.AddSingleton<IUserRepository>(new EfUserRepository(db));
        services.Services.AddSingleton<IQuizRepository>(new EfQuizRepository(db));
        services.Services.AddSingleton<IQuizAttemptRepository>(new EfQuizAttemptRepository(db));
        services.Services.AddSingleton<IUnitOfWork>(new EfUnitOfWork(db));
        var service = services.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);
        var paused = await service.PauseAsync(start.AttemptId, new SaveQuizProgressDto
        {
            Revision = 0, Answers = [new SubmitAnswerDto { QuestionId = question.Id, ResponseText = "Original" }]
        });
        db.ChangeTracker.Clear();
        var loaded = await service.GetProgressAsync(start.AttemptId);
        Assert.Equal(paused.PausedAt, loaded.PausedAt);
        Assert.Equal(paused.RemainingSeconds, loaded.RemainingSeconds);
        Assert.Equal(DateTimeKind.Utc, loaded.ExpiresAt.Kind);
        Assert.Equal(DateTimeKind.Utc, loaded.StartedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, loaded.PausedAt!.Value.Kind);
        Assert.Equal("Original", Assert.Single(loaded.Answers).ResponseText);
        Assert.Contains((await service.GetInProgressAsync(1, 10)).Items, item => item.AttemptId == start.AttemptId);

        var currentQuiz = await new EfQuizRepository(db).GetByIdAsync(quiz.Id, CancellationToken.None);
        currentQuiz!.QuizQuestions.Clear();
        currentQuiz.IsActive = false;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var resumed = await service.ResumeAsync(start.AttemptId, new AttemptRevisionDto { Revision = loaded.Revision });
        db.ChangeTracker.Clear();
        var result = await service.SubmitSavedAsync(start.AttemptId, new AttemptRevisionDto { Revision = resumed.Revision });
        db.ChangeTracker.Clear();
        Assert.Equal(100, result.Score);
        Assert.Equal(100, (await service.GetResultAsync(start.AttemptId)).Score);
        Assert.Equal("Original", Assert.Single((await service.GetResultAsync(start.AttemptId)).Answers).ResponseText);
    }

    [SqlServerFact]
    public async Task Concurrent_draft_updates_cannot_overwrite_each_other()
    {
        await using var first = database.CreateContext();
        var attempt = TestEntities.Attempt(TestEntities.Quiz(), TestEntities.User());
        attempt.SubmitAt = null;
        first.Add(attempt);
        await first.SaveChangesAsync();
        await using var second = database.CreateContext();
        var stale = await second.QuizAttempts.SingleAsync(a => a.Id == attempt.Id);
        attempt.Revision++;
        attempt.PausedAt = attempt.StartedAt;
        await new EfUnitOfWork(first).SaveChangesAsync(CancellationToken.None);
        stale.Revision++;
        stale.LastSavedAt = DateTime.UtcNow;

        await Assert.ThrowsAsync<ConflictException>(() => new EfUnitOfWork(second).SaveChangesAsync(CancellationToken.None));

        await using var verification = database.CreateContext();
        var saved = await verification.QuizAttempts.SingleAsync(a => a.Id == attempt.Id);
        Assert.NotNull(saved.PausedAt);
        Assert.Null(saved.LastSavedAt);
    }

    [SqlServerFact]
    public async Task Start_and_submit_persist_one_attempt_and_its_answers_across_contexts()
    {
        await using var db = database.CreateContext();
        var quiz = TestEntities.Quiz();
        quiz.IsActive = true;
        var user = TestEntities.User();
        user.IsActive = true;
        var question = TestEntities.Question(QuestionType.ShortAnswer);
        question.IsActive = true;
        quiz.Questions.Add(question);
        db.AddRange(quiz, user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        using var services = new ServiceTestContext();
        services.CurrentUser.UserId = user.Id;
        services.Services.AddSingleton<IUserRepository>(new EfUserRepository(db));
        services.Services.AddSingleton<IQuizRepository>(new EfQuizRepository(db));
        services.Services.AddSingleton<IQuizAttemptRepository>(new EfQuizAttemptRepository(db));
        services.Services.AddSingleton<IUnitOfWork>(new EfUnitOfWork(db));
        var service = services.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);
        Assert.Empty((await service.GetHistoryAsync(1, 10)).Items);
        db.ChangeTracker.Clear();

        var result = await service.SubmitAsync(quiz.Id, new SubmitQuizDto
        {
            AttemptId = start.AttemptId,
            Answers = [new SubmitAnswerDto { QuestionId = question.Id, ResponseText = "Test response" }]
        });

        await using var verification = database.CreateContext();
        var saved = await verification.QuizAttempts.Include(attempt => attempt.UserAnswers)
            .SingleAsync(attempt => attempt.Id == start.AttemptId);
        Assert.Equal(start.AttemptId, result.Id);
        Assert.Equal(start.StartedAt, saved.StartedAt);
        Assert.Equal(start.ExpiresAt, saved.ExpiresAt);
        Assert.Equal(result.SubmittedAt, saved.SubmitAt);
        Assert.NotEqual(Guid.Empty, Assert.Single(saved.UserAnswers).Id);
        Assert.Equal("Test response", saved.UserAnswers.Single().ResponseText);
        Assert.Equal(start.AttemptId, Assert.Single((await service.GetHistoryAsync(1, 10)).Items).Id);
    }

    [SqlServerFact]
    public async Task A_stale_submission_cannot_overwrite_an_already_submitted_attempt()
    {
        await using var firstContext = database.CreateContext();
        var quiz = TestEntities.Quiz();
        var user = TestEntities.User();
        var attempt = TestEntities.Attempt(quiz, user);
        attempt.SubmitAt = null;
        firstContext.Add(attempt);
        await firstContext.SaveChangesAsync();

        await using var secondContext = database.CreateContext();
        var stale = await secondContext.QuizAttempts.SingleAsync(row => row.Id == attempt.Id);
        attempt.SubmitAt = DateTime.UtcNow;
        attempt.Score = 40;
        await new EfUnitOfWork(firstContext).SaveChangesAsync(CancellationToken.None);

        stale.SubmitAt = DateTime.UtcNow;
        stale.Score = 80;
        await Assert.ThrowsAsync<ConflictException>(() =>
            new EfUnitOfWork(secondContext).SaveChangesAsync(CancellationToken.None));

        await using var verification = database.CreateContext();
        Assert.Equal(40, (await verification.QuizAttempts.SingleAsync(row => row.Id == attempt.Id)).Score);
    }

    [SqlServerTheory]
    [InlineData(QuestionType.SingleChoice)]
    [InlineData(QuestionType.MultipleChoice)]
    [InlineData(QuestionType.TrueFalse)]
    public async Task Fresh_quiz_load_supplies_choices_and_correct_submissions_receive_full_score(QuestionType type)
    {
        await using var db = database.CreateContext();
        var quiz = TestEntities.Quiz();
        quiz.IsActive = true;
        var user = TestEntities.User();
        user.IsActive = true;
        var question = TestEntities.Question(type);
        question.IsActive = true;
        var correct = new Answer
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, Text = "Correct option", IsCorrect = true, IsActive = true
        };
        var wrong = new Answer
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, Text = "Wrong option", IsCorrect = false, IsActive = true
        };
        var inactive = new Answer
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, Text = "Inactive option", IsCorrect = true, IsActive = false
        };
        question.Answers.Add(correct);
        question.Answers.Add(wrong);
        question.Answers.Add(inactive);
        quiz.Questions.Add(question);
        db.AddRange(quiz, user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // Verify the quiz repository itself loads answers, before the attempt repository can load them.
        var loaded = await new EfQuizRepository(db).GetByIdAsync(quiz.Id, CancellationToken.None);
        Assert.Equal(3, loaded!.QuizQuestions.Single().QuestionNavigation.Answers.Count);
        db.ChangeTracker.Clear();

        using var services = new ServiceTestContext();
        services.CurrentUser.UserId = user.Id;
        services.Services.AddSingleton<IUserRepository>(new EfUserRepository(db));
        services.Services.AddSingleton<IQuizRepository>(new EfQuizRepository(db));
        services.Services.AddSingleton<IQuizAttemptRepository>(new EfQuizAttemptRepository(db));
        services.Services.AddSingleton<IUnitOfWork>(new EfUnitOfWork(db));
        var service = services.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);
        var options = Assert.Single(start.Quiz.Questions).Answers;
        Assert.Equal(2, options.Count);
        Assert.Contains(options, option => option.Id == correct.Id && option.Text == correct.Text);
        Assert.Contains(options, option => option.Id == wrong.Id && option.Text == wrong.Text);
        Assert.DoesNotContain(options, option => option.Id == inactive.Id);
        db.ChangeTracker.Clear();

        var result = await service.SubmitAsync(quiz.Id, new SubmitQuizDto
        {
            AttemptId = start.AttemptId,
            Answers = [new SubmitAnswerDto { QuestionId = question.Id, AnswerIds = [correct.Id] }]
        });

        Assert.Equal(100, result.Score);
        Assert.Equal(start.AttemptId, result.Id);
        Assert.Equal(correct.Id, Assert.Single(Assert.Single(result.Answers).SelectedAnswers).Id);
    }
}
