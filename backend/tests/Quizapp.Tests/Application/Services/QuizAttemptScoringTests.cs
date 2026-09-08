using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Application.Services.QuizTaking;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;
using Quizapp.Domain.Exceptions;
using Quizapp.Tests.Data;

namespace Quizapp.Tests.Application.Services;

public class QuizAttemptScoringTests
{
    [Theory]
    [InlineData(QuestionType.SingleChoice)]
    [InlineData(QuestionType.TrueFalse)]
    public async Task Selecting_every_option_is_rejected_without_completing_the_attempt(QuestionType type)
    {
        using var context = new ServiceTestContext();
        var quiz = CreateQuiz(context, type);
        var question = quiz.QuizQuestions.Single().QuestionNavigation;
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);
        var request = new SubmitQuizDto
        {
            AttemptId = start.AttemptId,
            Answers = [new SubmitAnswerDto
            {
                QuestionId = question.Id,
                AnswerIds = question.Answers.Where(answer => answer.IsActive).Select(answer => answer.Id).ToList()
            }]
        };

        await Assert.ThrowsAsync<ValidationException>(() => service.SubmitAsync(quiz.Id, request));

        var attempt = await context.Get<IQuizAttemptRepository>().GetByIdAsync(start.AttemptId, CancellationToken.None);
        Assert.Null(attempt!.SubmitAt);
        Assert.Empty(attempt.UserAnswers);
        request.Answers.Single().AnswerIds = [question.Answers.Single(answer => answer.IsActive && answer.IsCorrect).Id];
        Assert.Equal(100, (await service.SubmitAsync(quiz.Id, request)).Score);
    }

    [Theory]
    [InlineData(QuestionType.SingleChoice, "inactive")]
    [InlineData(QuestionType.SingleChoice, "foreign")]
    [InlineData(QuestionType.SingleChoice, "unknown")]
    [InlineData(QuestionType.SingleChoice, "text")]
    [InlineData(QuestionType.TrueFalse, "inactive")]
    [InlineData(QuestionType.TrueFalse, "foreign")]
    [InlineData(QuestionType.TrueFalse, "unknown")]
    [InlineData(QuestionType.TrueFalse, "text")]
    public async Task Choice_must_be_active_and_belong_to_the_question(QuestionType type, string invalidChoice)
    {
        using var context = new ServiceTestContext();
        var quiz = CreateQuiz(context, type);
        var question = quiz.QuizQuestions.Single().QuestionNavigation;
        var inactive = CreateAnswer(question.Id, isCorrect: true, isActive: false);
        question.Answers.Add(inactive);
        var otherQuestion = TestEntities.Question(type);
        var foreign = CreateAnswer(otherQuestion.Id, isCorrect: true);
        otherQuestion.Answers.Add(foreign);
        context.Questions.Add(otherQuestion);
        var submission = new SubmitAnswerDto { QuestionId = question.Id };
        if (invalidChoice == "text")
            submission.ResponseText = "A choice question cannot be answered with text.";
        else
            submission.AnswerIds = [invalidChoice switch
            {
                "inactive" => inactive.Id,
                "foreign" => foreign.Id,
                "unknown" => Guid.NewGuid(),
                _ => throw new ArgumentOutOfRangeException(nameof(invalidChoice))
            }];
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);

        await Assert.ThrowsAsync<ValidationException>(() => service.SubmitAsync(quiz.Id,
            new SubmitQuizDto { AttemptId = start.AttemptId, Answers = [submission] }));

        var saved = await context.Get<IQuizAttemptRepository>().GetByIdAsync(start.AttemptId, CancellationToken.None);
        Assert.Null(saved!.SubmitAt);
        Assert.Empty(saved.UserAnswers);
    }

    [Theory]
    [InlineData(QuestionType.SingleChoice, "correct", 100)]
    [InlineData(QuestionType.SingleChoice, "wrong", 0)]
    [InlineData(QuestionType.SingleChoice, "unanswered", 0)]
    [InlineData(QuestionType.TrueFalse, "correct", 100)]
    [InlineData(QuestionType.TrueFalse, "wrong", 0)]
    [InlineData(QuestionType.TrueFalse, "unanswered", 0)]
    public async Task Valid_choices_and_unanswered_questions_keep_the_expected_score(QuestionType type,
        string selection, double expectedScore)
    {
        using var context = new ServiceTestContext();
        var quiz = CreateQuiz(context, type);
        var question = quiz.QuizQuestions.Single().QuestionNavigation;
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);
        var request = new SubmitQuizDto { AttemptId = start.AttemptId };
        if (selection != "unanswered")
            request.Answers.Add(new SubmitAnswerDto
            {
                QuestionId = question.Id,
                AnswerIds = [question.Answers.Single(answer => answer.IsCorrect == (selection == "correct")).Id]
            });

        Assert.Equal(expectedScore, (await service.SubmitAsync(quiz.Id, request)).Score);
    }

    [Theory]
    [InlineData(false, 100)]
    [InlineData(true, 50)]
    public async Task Multiple_choice_still_accepts_multiple_selections_and_penalizes_wrong_choices(bool includeWrong,
        double expectedScore)
    {
        using var context = new ServiceTestContext();
        var quiz = CreateQuiz(context, QuestionType.MultipleChoice);
        var question = quiz.QuizQuestions.Single().QuestionNavigation;
        question.Answers.Add(CreateAnswer(question.Id, isCorrect: true));
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);

        var result = await service.SubmitAsync(quiz.Id, new SubmitQuizDto
        {
            AttemptId = start.AttemptId,
            Answers = [new SubmitAnswerDto
            {
                QuestionId = question.Id,
                AnswerIds = question.Answers.Where(answer => answer.IsCorrect || includeWrong)
                    .Select(answer => answer.Id).ToList()
            }]
        });

        Assert.Equal(expectedScore, result.Score);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("foreign")]
    [InlineData("unknown")]
    public async Task Multiple_choice_answer_IDs_must_be_active_and_belong_to_the_question(string invalidChoice)
    {
        using var context = new ServiceTestContext();
        var quiz = CreateQuiz(context, QuestionType.MultipleChoice);
        var question = quiz.QuizQuestions.Single().QuestionNavigation;
        var inactive = CreateAnswer(question.Id, isCorrect: true, isActive: false);
        question.Answers.Add(inactive);
        var otherQuestion = TestEntities.Question(QuestionType.MultipleChoice);
        var foreign = CreateAnswer(otherQuestion.Id, isCorrect: true);
        otherQuestion.Answers.Add(foreign);
        context.Questions.Add(otherQuestion);
        var invalidId = invalidChoice switch
        {
            "inactive" => inactive.Id,
            "foreign"  => foreign.Id,
            "unknown"  => Guid.NewGuid(),
            _          => throw new ArgumentOutOfRangeException(nameof(invalidChoice))
        };
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);

        await Assert.ThrowsAsync<ValidationException>(() => service.SubmitAsync(quiz.Id,
            new SubmitQuizDto
            {
                AttemptId = start.AttemptId,
                Answers = [new SubmitAnswerDto { QuestionId = question.Id, AnswerIds = [invalidId] }]
            }));

        var saved = await context.Get<IQuizAttemptRepository>().GetByIdAsync(start.AttemptId, CancellationToken.None);
        Assert.Null(saved!.SubmitAt);
        Assert.Empty(saved.UserAnswers);
    }

    private static Quiz CreateQuiz(ServiceTestContext context, QuestionType type)
    {
        var quiz = TestEntities.Quiz();
        quiz.IsActive = true;
        var question = TestEntities.Question(type);
        question.IsActive = true;
        question.Answers.Add(CreateAnswer(question.Id, isCorrect: true));
        question.Answers.Add(CreateAnswer(question.Id, isCorrect: false));
        quiz.QuizQuestions.Add(new QuizQuestion
        {
            Id = Guid.NewGuid(), QuizId = quiz.Id, QuestionId = question.Id,
            QuizNavigation = quiz, QuestionNavigation = question
        });
        context.Quizzes.Add(quiz);
        return quiz;
    }

    private static Answer CreateAnswer(Guid questionId, bool isCorrect, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(), QuestionId = questionId, Text = "Test option",
        IsCorrect = isCorrect, IsActive = isActive
    };
}
