using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Application.DTOs.QuizManager.QuizQuestion;
using Quizapp.Application.DTOs.QuizManager.Quizzes;
using Quizapp.Application.Services.QuestionManager;
using Quizapp.Application.Services.QuizManager;
using Quizapp.Domain.Enums;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Tests.Application.Services;

public class QuizManagementServiceTests
{
    [Fact]
    public async Task A_bank_question_can_be_assigned_removed_and_reused_without_deletion()
    {
        using var context = new ServiceTestContext();
        var questions = context.Get<IQuestionService>();
        var quizzes = context.Get<IQuizService>();

        var question = await questions.CreateAsync(new CreateQuestionDto
        {
            Content = "C# keyword?", Level = QuestionLevel.Easy, QuestionType = QuestionType.SingleChoice,
            IsActive = true, Answers = [new() { Text = "class", IsCorrect = true }, new() { Text = "select" }]
        });

        var quiz = await quizzes.CreateAsync(new CreateQuizDto
            { Title = "C# basics", Duration = 15, PassedScore = 1, IsActive = true });

        var assignment = new AddQuestionToQuizDto { QuizId = quiz.Id, QuestionId = question.Id };
        var first = await quizzes.AddQuestionAsync(assignment);
        await Assert.ThrowsAsync<ConflictException>(() => quizzes.AddQuestionAsync(assignment));
        await quizzes.RemoveQuestionAsync(quiz.Id, question.Id);
        var second = await quizzes.AddQuestionAsync(assignment);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(question.Id, second.QuestionId);
        Assert.Equal("C# keyword?", (await questions.GetByIdAsync(question.Id)).Content);
    }
}
