using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Quizapp.Application;
using Quizapp.Application.DTOs.Authentication;
using Quizapp.Application.DTOs.QuizManager.Answers;
using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Application.DTOs.QuizManager.Quizzes;
using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Application.DTOs.UserManager;
using Quizapp.Domain.Enums;

namespace Quizapp.Tests.Application;

public class RequestValidationTests
{
    [Theory]
    [InlineData(QuestionType.MultipleChoice, true)]
    [InlineData(QuestionType.TrueFalse, true)]
    [InlineData(QuestionType.SingleChoice, true)]
    [InlineData(QuestionType.FillInTheBlanks, true)]
    [InlineData(QuestionType.ShortAnswer, true)]
    [InlineData(QuestionType.LongAnswer, true)]
    [InlineData((QuestionType)0, false)]
    [InlineData((QuestionType)7, false)]
    public void Questions_accept_only_supported_question_types(QuestionType type, bool valid)
    {
        Assert.Equal(valid, Validate(new CreateQuestionDto("Question", type, true, QuestionLevel.Easy)).IsValid);
        Assert.Equal(valid, Validate(new UpdateQuestionDto("Question", type, true, QuestionLevel.Easy)).IsValid);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(5.5, true)]
    [InlineData(120, true)]
    [InlineData(-1, false)]
    [InlineData(double.NaN, false)]
    [InlineData(double.PositiveInfinity, false)]
    public void Passed_score_is_an_absolute_finite_non_negative_number(double score, bool valid)
    {
        Assert.Equal(valid, Validate(new CreateQuizDto("Quiz", null, 15, null, false, score)).IsValid);
        Assert.Equal(valid, Validate(new UpdateQuizDto("Quiz", null, 15, null, false, score)).IsValid);
    }

    [Theory]
    [InlineData(QuestionLevel.Easy, true)]
    [InlineData(QuestionLevel.Medium, true)]
    [InlineData(QuestionLevel.Hard, true)]
    [InlineData((QuestionLevel)0, false)]
    [InlineData((QuestionLevel)4, false)]
    public void Questions_require_one_of_the_three_levels(QuestionLevel level, bool valid)
    {
        Assert.Equal(valid, Validate(new CreateQuestionDto("Question", QuestionType.SingleChoice, true, level)).IsValid);
        Assert.Equal(valid, Validate(new UpdateQuestionDto("Question", QuestionType.SingleChoice, true, level)).IsValid);
    }

    [Fact]
    public void Registration_validates_nested_profile_and_password_confirmation()
    {
        var request = new RegisterDto("student", "student@example.com", "test-only-password", "different-password",
            new UserProfileDto
            {
                FullName = "",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)
            });

        var errors = Validate(request).Errors.Select(error => error.PropertyName).ToArray();

        Assert.Contains(nameof(RegisterDto.ConfirmPassword), errors);
        Assert.Contains("Profile.FullName", errors);
        Assert.Contains("Profile.DateOfBirth", errors);
        Assert.False(Validate(new RegisterDto("student", "student@example.com", "password", "password", null!)).IsValid);
        Assert.True(Validate(new RegisterDto("student", "student@example.com", "password", "password",
            new UserProfileDto { FullName = "Student Name", DateOfBirth = new DateOnly(2000, 2, 29) })).IsValid);
    }

    [Fact]
    public void Submissions_accept_distinct_options_or_text_and_allow_unanswered_questions()
    {
        var questionId = Guid.NewGuid();
        var optionId = Guid.NewGuid();

        Assert.True(Validate(new SubmitAnswerDto { QuestionId = questionId, AnswerIds = [optionId, Guid.NewGuid()] }).IsValid);
        Assert.True(Validate(new SubmitAnswerDto { QuestionId = questionId, ResponseText = "An explanation" }).IsValid);
        Assert.True(Validate(new SubmitQuizDto()).IsValid);
        Assert.False(Validate(new SubmitAnswerDto { QuestionId = questionId }).IsValid);
        Assert.False(Validate(new SubmitAnswerDto { QuestionId = questionId, ResponseText = "  " }).IsValid);
        Assert.False(Validate(new SubmitAnswerDto { QuestionId = questionId, AnswerIds = [optionId, optionId] }).IsValid);
        Assert.False(Validate(new SubmitAnswerDto { QuestionId = questionId, AnswerIds = [optionId], ResponseText = "text" }).IsValid);
        Assert.False(Validate(new SubmitAnswerDto { QuestionId = questionId, AnswerIds = null! }).IsValid);
    }

    [Fact]
    public void Submissions_reject_duplicate_questions_and_null_items()
    {
        var answer = new SubmitAnswerDto { QuestionId = Guid.NewGuid(), AnswerIds = [Guid.NewGuid()] };

        Assert.False(Validate(new SubmitQuizDto { Answers = [answer, answer] }).IsValid);
        Assert.False(Validate(new SubmitQuizDto { Answers = [null!] }).IsValid);
        Assert.False(Validate(new SubmitQuizDto { Answers = null! }).IsValid);
    }

    [Fact]
    public void Role_assignments_allow_clearing_roles_but_reject_duplicates_and_empty_ids()
    {
        var roleId = Guid.NewGuid();

        Assert.True(Validate(new UpdateUserRolesDto()).IsValid);
        Assert.True(Validate(new UpdateUserRolesDto { RoleIds = [roleId, Guid.NewGuid()] }).IsValid);
        Assert.False(Validate(new UpdateUserRolesDto { RoleIds = [roleId, roleId] }).IsValid);
        Assert.False(Validate(new UpdateUserRolesDto { RoleIds = [Guid.Empty] }).IsValid);
        Assert.False(Validate(new UpdateUserRolesDto { RoleIds = null! }).IsValid);
    }

    [Fact]
    public void Incorrect_and_inactive_answer_options_are_valid_management_input()
    {
        Assert.True(Validate(new CreateAnswerDto("An incorrect option", false, false, Guid.NewGuid())).IsValid);
        Assert.True(Validate(new UpdateAnswerDto("An incorrect option", false, false)).IsValid);
    }

    private static FluentValidation.Results.ValidationResult Validate<T>(T request)
    {
        using var provider = new ServiceCollection().AddApplication().BuildServiceProvider();
        using var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IValidator<T>>().Validate(request);
    }
}
