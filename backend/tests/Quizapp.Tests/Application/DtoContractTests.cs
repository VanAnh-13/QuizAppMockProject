using System.Text.Json;
using Quizapp.Application.DTOs.Authentication;
using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Application.DTOs.UserManager;
using Quizapp.Application.Validators.Authentication;

namespace Quizapp.Tests.Application;

public class DtoContractTests
{
    [Fact]
    public void Missing_json_fields_use_defaults_but_registration_still_requires_valid_data()
    {
        var request = JsonSerializer.Deserialize<RegisterDto>("{}")!;

        Assert.Equal(string.Empty, request.Username);
        Assert.Equal(string.Empty, request.Email);
        Assert.NotNull(request.Profile);
        Assert.Equal(string.Empty, request.Profile.FullName);

        var result = new RegisterDtoValidator().Validate(request);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterDto.Username));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterDto.Email));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterDto.Password));
        Assert.Contains(result.Errors, error => error.PropertyName == "Profile.FullName");
    }

    [Fact]
    public void Registration_accepts_only_profile_data_from_the_nested_user_object()
    {
        const string json = """
            {
              "Username": "student",
              "Email": "student@example.com",
              "Password": "test-only-password",
              "ConfirmPassword": "test-only-password",
              "IsActive": true,
              "Roles": ["Admin"],
              "Profile": {
                "FullName": "Student Name",
                "Roles": ["Admin"],
                "Status": 1
              }
            }
            """;

        var request = JsonSerializer.Deserialize<RegisterDto>(json)!;
        var acceptedFields = JsonSerializer.Serialize(request);

        Assert.Equal("Student Name", request.Profile.FullName);
        Assert.DoesNotContain("Roles", acceptedFields);
        Assert.DoesNotContain("Status", acceptedFields);
        Assert.DoesNotContain("IsActive", acceptedFields);
    }

    [Fact]
    public void Public_quiz_and_user_responses_do_not_expose_answers_or_passwords()
    {
        var quiz = new QuizForAttemptDto
        {
            Title = "Quiz",
            Questions =
            [
                new QuestionForAttemptDto
                {
                    Content = "Question",
                    Answers = [new AnswerOptionDto { Id = Guid.NewGuid(), Text = "An option" }]
                }
            ]
        };
        var authentication = new AuthResponseDto
        {
            Token = "test-only-token",
            ExpiresAt = DateTime.UtcNow,
            UserDto = new UserDto { Id = Guid.NewGuid(), Username = "student", Email = "student@example.com" }
        };

        Assert.DoesNotContain("IsCorrect", JsonSerializer.Serialize(quiz));
        Assert.DoesNotContain("Password", JsonSerializer.Serialize(authentication));
        Assert.DoesNotContain("Score", JsonSerializer.Serialize(new SubmitQuizDto()));
        Assert.DoesNotContain("UserId", JsonSerializer.Serialize(new SubmitQuizDto()));
    }
}
