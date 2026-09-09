using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Application.DTOs.QuizManager.QuizQuestion;
using Quizapp.Application.DTOs.QuizManager.Quizzes;
using Quizapp.Application.DTOs.RoleManager;
using Quizapp.Application.DTOs.UserManager;
using Quizapp.Domain.Entities;

namespace Quizapp.Application.Services.Common;

internal static class DtoMapping
{
    public static RoleDto ToDto(Role role) => new()
    {
        Id = role.Id, RoleName = role.RoleName, Description = role.Description, IsActive = role.IsActive
    };

    public static UserDto ToDto(User user) => new()
    {
        Id = user.Id, Username = user.Username, Email = user.Email,
        FullName = user.FullName, PhoneNumber = user.PhoneNumber, DateOfBirth = user.DateOfBirth,
        Avatar = user.Avatar, IsActive = user.IsActive, CreatedAt = user.CreateAt, UpdatedAt = user.UpdateAt,
        Roles =
        [
            .. user.Roles.OrderBy(role => role.RoleName)
                .ThenBy(role => role.Id)
                .Select(ToDto)
        ]
    };

    public static QuizDto ToDto(Quiz quiz) => new()
    {
        Id = quiz.Id, Title = quiz.Title, Description = quiz.Description, Duration = quiz.Duration,
        Image = quiz.Image, PassedScore = quiz.PassedScore, IsActive = quiz.IsActive,
        CreatedAt = quiz.CreateAt, UpdatedAt = quiz.UpdateAt
    };

    public static QuestionDto ToDto(Question question) => new()
    {
        Id = question.Id, Content = question.Content, Image = question.Image,
        Level = question.Level, QuestionType = question.QuestionType, IsActive = question.IsActive
    };

    public static QuizQuestionDto ToDto(QuizQuestion assignment) => new()
    {
        Id = assignment.Id, QuizId = assignment.QuizId, QuestionId = assignment.QuestionId,
        Order = assignment.Order, Content = assignment.QuestionNavigation.Content,
        Image = assignment.QuestionNavigation.Image, Level = assignment.QuestionNavigation.Level,
        QuestionType = assignment.QuestionNavigation.QuestionType
    };
}
