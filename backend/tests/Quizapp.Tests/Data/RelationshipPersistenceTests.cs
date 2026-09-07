using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;

namespace Quizapp.Tests.Data;

public class RelationshipPersistenceTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    private const int ConstraintViolation = 547;
    private const int DuplicateIndex = 2601;
    private const int DuplicateKey = 2627;
    private const int LongResponseCharacterCount = 8192;

    [SqlServerFact]
    public async Task Removing_a_question_assignment_preserves_the_bank_and_other_quizzes()
    {
        await using var context = database.CreateContext();
        var firstQuiz = TestEntities.Quiz();
        var secondQuiz = TestEntities.Quiz();
        var question = TestEntities.Question();
        firstQuiz.Questions.Add(question);
        secondQuiz.Questions.Add(question);
        context.AddRange(firstQuiz, secondQuiz);
        await context.SaveChangesAsync();

        var assignment = await context.QuizQuestions.SingleAsync(value => value.QuizId == firstQuiz.Id);
        context.QuizQuestions.Remove(assignment);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.True(await context.Questions.AnyAsync(value => value.Id == question.Id));

        Assert.Empty(await context.QuizQuestions.Where(value => value.QuizId == firstQuiz.Id)
            .ToListAsync());

        Assert.Equal(question.Id, (await context.Quizzes.Include(value => value.Questions)
                .SingleAsync(value => value.Id == secondQuiz.Id)).Questions.Single()
            .Id);
    }

    [SqlServerFact]
    public async Task Duplicate_question_assignments_are_rejected()
    {
        await using var context = database.CreateContext();
        var quiz = TestEntities.Quiz();
        var question = TestEntities.Question();
        quiz.Questions.Add(question);
        context.Add(quiz);
        await context.SaveChangesAsync();

        context.QuizQuestions.Add(new QuizQuestion { QuizId = quiz.Id, QuestionId = question.Id });

        await AssertRejectedAsync(() => context.SaveChangesAsync(), DuplicateIndex, DuplicateKey);
    }

    [SqlServerFact]
    public async Task Deleting_an_unused_quiz_preserves_its_questions_and_options()
    {
        await using var context = database.CreateContext();
        var quiz = TestEntities.Quiz();
        var question = TestEntities.Question();
        var answer = TestEntities.Answer(question);
        quiz.Questions.Add(question);
        context.AddRange(quiz, answer);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await context.Quizzes.Where(value => value.Id == quiz.Id)
            .ExecuteDeleteAsync();

        Assert.True(await context.Questions.AnyAsync(value => value.Id == question.Id));
        Assert.True(await context.Answers.AnyAsync(value => value.Id == answer.Id));
        Assert.False(await context.QuizQuestions.AnyAsync(value => value.QuizId == quiz.Id));
    }

    [SqlServerFact]
    public async Task Deleting_an_unused_question_removes_assignments_and_options_but_preserves_the_quiz()
    {
        await using var context = database.CreateContext();
        var quiz = TestEntities.Quiz();
        var question = TestEntities.Question();
        var answer = TestEntities.Answer(question);
        quiz.Questions.Add(question);
        context.AddRange(quiz, answer);
        await context.SaveChangesAsync();

        await context.Questions.Where(value => value.Id == question.Id)
            .ExecuteDeleteAsync();

        Assert.True(await context.Quizzes.AnyAsync(value => value.Id == quiz.Id));
        Assert.False(await context.Answers.AnyAsync(value => value.Id == answer.Id));
        Assert.False(await context.QuizQuestions.AnyAsync(value => value.QuestionId == question.Id));
    }

    [SqlServerFact]
    public async Task Deleting_one_role_preserves_the_user_and_their_other_roles()
    {
        await using var context = database.CreateContext();
        var user = TestEntities.User();
        var firstRole = TestEntities.Role();
        var secondRole = TestEntities.Role();
        user.Roles.Add(firstRole);
        user.Roles.Add(secondRole);
        context.Add(user);
        await context.SaveChangesAsync();

        await context.Roles.Where(value => value.Id == firstRole.Id)
            .ExecuteDeleteAsync();

        context.ChangeTracker.Clear();

        var reloadedUser = await context.Users.Include(value => value.Roles)
            .SingleAsync(value => value.Id == user.Id);

        Assert.Equal(secondRole.Id, Assert.Single(reloadedUser.Roles)
            .Id);

        Assert.False(await context.UserRoles.AnyAsync(value => value.RoleId == firstRole.Id));
    }

    [SqlServerTheory]
    [InlineData(typeof(Quiz))]
    [InlineData(typeof(User))]
    [InlineData(typeof(Question))]
    [InlineData(typeof(Answer))]
    public async Task Deleting_entities_referenced_by_history_is_rejected(Type principalType)
    {
        await using var context = database.CreateContext();
        var quiz = TestEntities.Quiz();
        var user = TestEntities.User();
        var question = TestEntities.Question();
        var answer = TestEntities.Answer(question);
        var attempt = TestEntities.Attempt(quiz, user);
        quiz.Questions.Add(question);
        var selection = TestEntities.Selection(attempt, question, answer);
        context.AddRange(answer, selection);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        object principal = principalType.Name switch
        {
            nameof(Quiz) => quiz,
            nameof(User) => user,
            nameof(Question) => question,
            nameof(Answer) => answer,
            _ => throw new ArgumentOutOfRangeException(nameof(principalType))
        };

        context.Entry(principal)
            .State = EntityState.Deleted;

        await AssertRejectedAsync(() => context.SaveChangesAsync(), ConstraintViolation);
        Assert.True(await context.QuizAttempts.AnyAsync(value => value.Id == attempt.Id));
        Assert.True(await context.UserAnswers.AnyAsync(value => value.Id == selection.Id));
    }

    [SqlServerFact]
    public async Task Multiple_choice_keeps_distinct_options_but_rejects_a_duplicate_selection()
    {
        await using var context = database.CreateContext();
        var question = TestEntities.Question();
        var firstAnswer = TestEntities.Answer(question);
        var secondAnswer = TestEntities.Answer(question);
        var attempt = TestEntities.Attempt(TestEntities.Quiz(), TestEntities.User());
        attempt.QuizNavigation.Questions.Add(question);

        context.AddRange(firstAnswer, secondAnswer,
            TestEntities.Selection(attempt, question, firstAnswer),
            TestEntities.Selection(attempt, question, secondAnswer));

        await context.SaveChangesAsync();
        Assert.Equal(2, await context.UserAnswers.CountAsync(value => value.QuizAttemptId == attempt.Id));

        context.Add(TestEntities.Selection(attempt, question, firstAnswer));

        await AssertRejectedAsync(() => context.SaveChangesAsync(), DuplicateIndex, DuplicateKey);
    }

    [SqlServerFact]
    public async Task An_option_from_another_question_is_rejected_by_the_database()
    {
        await using var context = database.CreateContext();
        var question = TestEntities.Question();
        var unrelatedAnswer = TestEntities.Answer(TestEntities.Question());
        var attempt = TestEntities.Attempt(TestEntities.Quiz(), TestEntities.User());
        attempt.QuizNavigation.Questions.Add(question);
        context.AddRange(attempt, unrelatedAnswer);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        context.UserAnswers.Add(new UserAnswer
        {
            QuizAttemptId = attempt.Id,
            QuestionId = question.Id,
            AnswerId = unrelatedAnswer.Id
        });

        await AssertRejectedAsync(() => context.SaveChangesAsync(), ConstraintViolation);
    }

    [SqlServerTheory]
    [InlineData(QuestionType.FillInTheBlanks)]
    [InlineData(QuestionType.ShortAnswer)]
    [InlineData(QuestionType.LongAnswer)]
    public async Task Text_responses_are_saved_without_an_option_and_cannot_be_duplicated(QuestionType type)
    {
        await using var context = database.CreateContext();
        var question = TestEntities.Question(type);
        var attempt = TestEntities.Attempt(TestEntities.Quiz(), TestEntities.User());
        attempt.QuizNavigation.Questions.Add(question);
        context.Add(TestEntities.Selection(attempt, question, responseText: "Written response"));
        await context.SaveChangesAsync();

        var response = await context.UserAnswers.SingleAsync(value => value.QuizAttemptId == attempt.Id);
        Assert.Null(response.AnswerId);
        Assert.Equal("Written response", response.ResponseText);
        context.Add(TestEntities.Selection(attempt, question, responseText: "Duplicate response"));

        await AssertRejectedAsync(() => context.SaveChangesAsync(), DuplicateIndex, DuplicateKey);
    }

    [SqlServerFact]
    public async Task Long_answer_text_round_trips_without_truncation()
    {
        await using var context = database.CreateContext();
        var question = TestEntities.Question(QuestionType.LongAnswer);
        var attempt = TestEntities.Attempt(TestEntities.Quiz(), TestEntities.User());
        var responseText = new string('\u0103', LongResponseCharacterCount);
        attempt.QuizNavigation.Questions.Add(question);
        context.Add(TestEntities.Selection(attempt, question, responseText: responseText));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var savedResponse = await context.UserAnswers.SingleAsync(value => value.QuizAttemptId == attempt.Id);

        Assert.Equal(responseText, savedResponse.ResponseText);
    }

    [SqlServerTheory]
    [InlineData(false, null)]
    [InlineData(false, "")]
    [InlineData(false, "   ")]
    [InlineData(true, "Option and text together")]
    public async Task Empty_or_ambiguous_responses_are_rejected(bool selectOption, string? text)
    {
        await using var context = database.CreateContext();
        var question = TestEntities.Question();
        var answer = TestEntities.Answer(question);
        var attempt = TestEntities.Attempt(TestEntities.Quiz(), TestEntities.User());
        attempt.QuizNavigation.Questions.Add(question);
        context.AddRange(answer, attempt);
        await context.SaveChangesAsync();
        context.Add(TestEntities.Selection(attempt, question, selectOption ? answer : null, text));

        await AssertRejectedAsync(() => context.SaveChangesAsync(), ConstraintViolation);
    }

    private static async Task AssertRejectedAsync(Func<Task> save, params int[] expectedNumbers)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(save);
        var sqlException = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Contains(sqlException.Number, expectedNumbers);
    }
}
