using Microsoft.EntityFrameworkCore;
using Quizapp.Application.Services.QuizManager;
using Quizapp.Domain.Entities;
using Quizapp.Infrastructure.Persistence;
using Quizapp.Tests.Application.Services;

namespace Quizapp.Tests.Data;

public class QuizQuestionReorderTests
{
    [Fact]
    public async Task Reorder_updates_tracked_assignments_without_replacing_them()
    {
        using var services = new ServiceTestContext();
        await using var db = new QuizAppDbContext(new DbContextOptionsBuilder<QuizAppDbContext>()
            .UseSqlServer().Options);
        var quiz = CreateQuizWithQuestions();
        var original = quiz.QuizQuestions.OrderBy(assignment => assignment.Order).ToArray();
        services.Quizzes.Add(quiz);
        db.Attach(quiz);

        await services.Get<IQuizService>().ReorderQuestionAsync(quiz.Id,
            [original[1].QuestionId, original[0].QuestionId]);

        db.ChangeTracker.DetectChanges();
        var tracked = db.ChangeTracker.Entries<QuizQuestion>().ToArray();
        Assert.Equal(2, tracked.Length);
        Assert.All(tracked, entry =>
        {
            Assert.Equal(EntityState.Modified, entry.State);
            Assert.Equal(nameof(QuizQuestion.Order), Assert.Single(entry.Properties, p => p.IsModified).Metadata.Name);
        });
        Assert.Same(original[1], quiz.QuizQuestions.Single(assignment => assignment.Order == 1));
        Assert.Same(original[0], quiz.QuizQuestions.Single(assignment => assignment.Order == 2));
        Assert.All(db.ChangeTracker.Entries<Question>(), entry => Assert.Equal(EntityState.Unchanged, entry.State));
        Assert.Equal(EntityState.Unchanged, db.Entry(quiz).State);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("unknown")]
    [InlineData("missing")]
    [InlineData("extra")]
    public async Task Invalid_reorder_is_rejected_without_changing_assignments(string input)
    {
        using var services = new ServiceTestContext();
        var quiz = CreateQuizWithQuestions();
        services.Quizzes.Add(quiz);
        var original = quiz.QuizQuestions.Select(assignment => (assignment.Id, assignment.Order)).ToArray();
        var questionIds = quiz.QuizQuestions.Select(assignment => assignment.QuestionId).ToArray();
        Guid[] requested = input switch
        {
            "duplicate" => [questionIds[0], questionIds[0]],
            "unknown" => [questionIds[0], Guid.NewGuid()],
            "missing" => [questionIds[0]],
            "extra" => [questionIds[0], questionIds[1], Guid.NewGuid()],
            _ => throw new ArgumentOutOfRangeException(nameof(input))
        };

        await Assert.ThrowsAsync<Quizapp.Domain.Exceptions.ValidationException>(() =>
            services.Get<IQuizService>().ReorderQuestionAsync(quiz.Id, requested));

        Assert.Equal(original, quiz.QuizQuestions.Select(assignment => (assignment.Id, assignment.Order)));
    }

    [Fact]
    public async Task Reordering_an_empty_quiz_with_an_empty_list_is_valid()
    {
        using var services = new ServiceTestContext();
        var quiz = TestEntities.Quiz();
        services.Quizzes.Add(quiz);

        await services.Get<IQuizService>().ReorderQuestionAsync(quiz.Id, []);

        Assert.Empty(quiz.QuizQuestions);
    }

    private static Quiz CreateQuizWithQuestions()
    {
        var quiz = TestEntities.Quiz();
        for (var order = 1; order <= 2; order++)
        {
            var question = TestEntities.Question();
            quiz.QuizQuestions.Add(new QuizQuestion
            {
                Id = Guid.NewGuid(), QuizId = quiz.Id, QuestionId = question.Id,
                QuizNavigation = quiz, QuestionNavigation = question, Order = order
            });
        }
        return quiz;
    }
}
