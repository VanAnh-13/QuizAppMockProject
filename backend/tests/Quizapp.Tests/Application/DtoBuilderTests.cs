using System.Text.Json;
using Quizapp.Application.DTOs.Authentication;
using Quizapp.Application.DTOs.QuizHistory;
using Quizapp.Application.DTOs.QuizManager.Answers;
using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Application.DTOs.QuizManager.QuizQuestion;
using Quizapp.Application.DTOs.QuizManager.Quizzes;
using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Application.DTOs.UserManager;
using Quizapp.Application.Validators.QuizManager.Quizzes;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;

namespace Quizapp.Tests.Application;

public class DtoBuilderTests
{
    [Fact]
    public void Quiz_builder_uses_defaults_and_creates_independent_mutable_results()
    {
        var builder = new CreateQuizDto.Builder().WithTitle("C# basics")
            .WithDuration(30);

        var first = builder.Build();

        var second = builder.WithTitle("Advanced C#")
            .WithPassedScore(120)
            .Build();

        Assert.Equal("C# basics", first.Title);
        Assert.Equal(30, first.Duration);
        Assert.Equal(0, first.PassedScore);
        Assert.False(first.IsActive);
        Assert.Null(first.Description);
        Assert.Equal("Advanced C#", second.Title);
        Assert.Equal(120, second.PassedScore);
        Assert.NotSame(first, second);
        first.Title = "Edited after creation";
        Assert.Equal("Advanced C#", second.Title);

        Assert.Equal("Advanced C#", builder.Build()
            .Title);
    }

    [Fact]
    public void Submission_builder_snapshots_collections_at_assignment_and_each_build()
    {
        var questionId = Guid.NewGuid();
        var answerId = Guid.NewGuid();
        var input = new List<Guid> { answerId };

        var builder = new SubmitAnswerDto.Builder().WithQuestionId(questionId)
            .WithAnswerIds(input);

        input.Clear();

        var first = builder.Build();
        Assert.Equal(answerId, Assert.Single(first.AnswerIds));
        first.AnswerIds.Clear();

        var second = builder.Build();
        Assert.Equal(answerId, Assert.Single(second.AnswerIds));
        Assert.Equal(questionId, second.QuestionId);

        var attemptId = Guid.NewGuid();
        var submissionBuilder = new SubmitQuizDto.Builder().WithAttemptId(attemptId).WithAnswers([second]);
        Assert.Equal(attemptId, submissionBuilder.Build().AttemptId);
        var submission = submissionBuilder.Build();
        submission.Answers.Clear();

        Assert.Single(submissionBuilder.Build()
            .Answers);

        Assert.Empty(new SubmitQuizDto.Builder().Build()
            .Answers);

        Assert.Throws<ArgumentNullException>(() => builder.WithAnswerIds(null!));
    }

    [Fact]
    public void Unset_values_use_dto_defaults_and_can_be_assigned_after_creation()
    {
        var login = new LoginDto.Builder().Build();
        Assert.Equal(string.Empty, login.Username);
        Assert.Equal(string.Empty, login.Password);
        login.Username = "student";
        Assert.Equal("student", login.Username);
        Assert.Throws<ArgumentNullException>(() => new LoginDto.Builder().WithPassword(null!));

        var answer = new CreateAnswerDto.Builder().WithText("An incorrect option");

        Assert.False(answer.Build()
            .IsCorrect);

        var question = new CreateQuestionDto.Builder().WithContent("A question");

        Assert.Equal((QuestionLevel)0, question.Build()
            .Level);

        Assert.Equal(QuestionLevel.Easy, question.WithLevel(QuestionLevel.Easy)
            .Build()
            .Level);
    }

    [Fact]
    public void Builders_preserve_defaults_and_leave_business_rules_to_validators()
    {
        var assignment = new AddQuestionToQuizDto.Builder()
            .WithQuizId(Guid.NewGuid())
            .WithQuestionId(Guid.NewGuid())
            .Build();

        Assert.Equal(QuizQuestion.FirstOrder, assignment.Order);

        var invalidQuiz = new CreateQuizDto.Builder()
            .WithTitle("Quiz")
            .WithDuration(-1)
            .WithPassedScore(-1)
            .Build();

        Assert.False(new CreateQuizDtoValidator().Validate(invalidQuiz)
            .IsValid);

        var legacyQuiz = new QuizDto.Builder().WithTitle("Legacy quiz")
            .WithDuration(30)
            .Build();

        Assert.Null(legacyQuiz.PassedScore);
    }

    [Fact]
    public void Nested_builder_results_preserve_public_json_contracts()
    {
        var profile = new UserProfileDto.Builder().WithFullName("Student Name")
            .Build();

        var registration = new RegisterDto.Builder().WithUsername("student")
            .WithEmail("student@example.com")
            .WithPassword("test-only-password")
            .WithConfirmPassword("test-only-password")
            .WithProfile(profile)
            .Build();

        var registrationJson = JsonSerializer.Serialize(registration);
        Assert.Contains("Student Name", registrationJson);
        Assert.DoesNotContain("Roles", registrationJson);
        Assert.DoesNotContain("IsActive", registrationJson);

        var authentication = new AuthResponseDto.Builder().WithToken("test-only-token")
            .WithUserDto(new UserDto.Builder().WithUsername("student")
                .WithEmail("student@example.com")
                .Build())
            .Build();

        Assert.DoesNotContain("Password", JsonSerializer.Serialize(authentication));

        var quiz = new QuizForAttemptDto.Builder().WithTitle("Quiz")
            .WithQuestions([
                new QuestionForAttemptDto.Builder().WithContent("Question")
                    .WithAnswers([
                        new AnswerOptionDto.Builder().WithText("Option")
                            .Build()
                    ])
                    .Build()
            ])
            .Build();

        var quizJson = JsonSerializer.Serialize(quiz);
        Assert.Contains("Option", quizJson);
        Assert.DoesNotContain("IsCorrect", quizJson);

        var submissionJson = JsonSerializer.Serialize(new SubmitQuizDto.Builder().Build());
        Assert.DoesNotContain("Score", submissionJson);
        Assert.DoesNotContain("UserId", submissionJson);
    }

    [Fact]
    public void History_detail_builder_includes_inherited_fields_and_nullable_legacy_data()
    {
        var attemptId = Guid.NewGuid();
        var quizId = Guid.NewGuid();
        var submittedAt = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);

        var result = new UserAnswerResultDto.Builder().WithQuestionContent("Legacy question")
            .WithResponseText("An explanation")
            .Build();

        var history = new QuizAttemptDetailDto.Builder().WithId(attemptId)
            .WithQuizId(quizId)
            .WithQuizTitle("History quiz")
            .WithSubmittedAt(submittedAt)
            .WithScore(7.5)
            .WithAnswers([result])
            .Build();

        Assert.Equal(attemptId, history.Id);
        Assert.Equal(quizId, history.QuizId);
        Assert.Equal("History quiz", history.QuizTitle);
        Assert.Equal(submittedAt, history.SubmittedAt);
        Assert.Equal(7.5, history.Score);

        Assert.Equal("An explanation", Assert.Single(history.Answers)
            .ResponseText);

        Assert.Null(result.Level);
        Assert.Null(result.Image);
    }
}
