using QuizApp.Application.DTOs.QuizCodes;
using QuizApp.Application.Interfaces;
using QuizApp.Application.Services;
using QuizApp.Domain.Enums;
using Xunit;

namespace QuizApp.Application.Tests;

/// <summary>
/// Unit tests for ScoringService covering all six QuestionType rules
/// and edge cases required by the spec.
/// </summary>
public class ScoringServiceTests
{
    private readonly IScoringService _sut = new ScoringService();

    // ---- Helpers -------------------------------------------------------

    private static QuestionForScoring SingleChoice(params (Guid id, string content, bool correct)[] answers) =>
        new()
        {
            QuestionId = Guid.NewGuid(),
            QuestionContent = "Q",
            QuestionType = QuestionType.SingleChoice,
            Answers = answers.Select(a => new AnswerForScoring
            {
                AnswerId = a.id, Content = a.content, IsCorrect = a.correct
            }).ToList()
        };

    private static QuestionForScoring TrueFalse(Guid trueId, Guid falseId, bool correctIsTrue) =>
        new()
        {
            QuestionId = Guid.NewGuid(),
            QuestionContent = "Q",
            QuestionType = QuestionType.TrueFalse,
            Answers =
            [
                new AnswerForScoring { AnswerId = trueId, Content = "True", IsCorrect = correctIsTrue },
                new AnswerForScoring { AnswerId = falseId, Content = "False", IsCorrect = !correctIsTrue }
            ]
        };

    private static QuestionForScoring MultipleChoice(params (Guid id, string content, bool correct)[] answers) =>
        new()
        {
            QuestionId = Guid.NewGuid(),
            QuestionContent = "Q",
            QuestionType = QuestionType.MultipleChoice,
            Answers = answers.Select(a => new AnswerForScoring
            {
                AnswerId = a.id, Content = a.content, IsCorrect = a.correct
            }).ToList()
        };

    private static QuestionForScoring TextQuestion(QuestionType type, string correctText) =>
        new()
        {
            QuestionId = Guid.NewGuid(),
            QuestionContent = "Q",
            QuestionType = type,
            Answers = [new AnswerForScoring { AnswerId = Guid.NewGuid(), Content = correctText, IsCorrect = true }]
        };

    private static QuestionForScoring LongAnswer() =>
        new()
        {
            QuestionId = Guid.NewGuid(),
            QuestionContent = "Q",
            QuestionType = QuestionType.LongAnswer,
            Answers = []
        };

    // ---- SingleChoice --------------------------------------------------

    [Fact]
    public void SingleChoice_CorrectAnswer_Scores()
    {
        var correctId = Guid.NewGuid();
        var wrongId = Guid.NewGuid();
        var q = SingleChoice((correctId, "A", true), (wrongId, "B", false));

        var result = _sut.Score([q], [new UserAnswerSubmissionViewModel
        {
            QuestionId = q.QuestionId.ToString(),
            AnswerId = correctId.ToString()
        }]);

        Assert.Equal(1, result.CorrectCount);
        Assert.Equal(1, result.GradableCount);
        Assert.Equal(100, result.Score);
        Assert.True(result.UserAnswers[0].IsCorrect);
    }

    [Fact]
    public void SingleChoice_WrongAnswer_DoesNotScore()
    {
        var correctId = Guid.NewGuid();
        var wrongId = Guid.NewGuid();
        var q = SingleChoice((correctId, "A", true), (wrongId, "B", false));

        var result = _sut.Score([q], [new UserAnswerSubmissionViewModel
        {
            QuestionId = q.QuestionId.ToString(),
            AnswerId = wrongId.ToString()
        }]);

        Assert.Equal(0, result.CorrectCount);
        Assert.Equal(0, result.Score);
        Assert.False(result.UserAnswers[0].IsCorrect);
    }

    [Fact]
    public void SingleChoice_NoAnswer_DoesNotScore()
    {
        var correctId = Guid.NewGuid();
        var q = SingleChoice((correctId, "A", true));

        // Empty submission
        var result = _sut.Score([q], []);

        Assert.Equal(0, result.CorrectCount);
        Assert.Equal(0, result.Score);
    }

    // ---- TrueFalse -----------------------------------------------------

    [Fact]
    public void TrueFalse_CorrectPick_Scores()
    {
        var trueId = Guid.NewGuid();
        var falseId = Guid.NewGuid();
        var q = TrueFalse(trueId, falseId, correctIsTrue: true);

        var result = _sut.Score([q], [new UserAnswerSubmissionViewModel
        {
            QuestionId = q.QuestionId.ToString(),
            AnswerId = trueId.ToString()
        }]);

        Assert.True(result.UserAnswers[0].IsCorrect);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void TrueFalse_WrongPick_DoesNotScore()
    {
        var trueId = Guid.NewGuid();
        var falseId = Guid.NewGuid();
        var q = TrueFalse(trueId, falseId, correctIsTrue: true);

        var result = _sut.Score([q], [new UserAnswerSubmissionViewModel
        {
            QuestionId = q.QuestionId.ToString(),
            AnswerId = falseId.ToString()
        }]);

        Assert.False(result.UserAnswers[0].IsCorrect);
        Assert.Equal(0, result.Score);
    }

    // ---- MultipleChoice ------------------------------------------------

    [Fact]
    public void MultipleChoice_ExactSet_Scores()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var c = Guid.NewGuid();
        var q = MultipleChoice((a, "A", true), (b, "B", true), (c, "C", false));

        var result = _sut.Score([q], [new UserAnswerSubmissionViewModel
        {
            QuestionId = q.QuestionId.ToString(),
            AnswerIds = [a.ToString(), b.ToString()]
        }]);

        Assert.True(result.UserAnswers[0].IsCorrect);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void MultipleChoice_PartialSubset_DoesNotScore()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var c = Guid.NewGuid();
        var q = MultipleChoice((a, "A", true), (b, "B", true), (c, "C", false));

        var result = _sut.Score([q], [new UserAnswerSubmissionViewModel
        {
            QuestionId = q.QuestionId.ToString(),
            AnswerIds = [a.ToString()]   // missing b
        }]);

        Assert.False(result.UserAnswers[0].IsCorrect);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void MultipleChoice_Superset_DoesNotScore()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var c = Guid.NewGuid();
        var q = MultipleChoice((a, "A", true), (b, "B", true), (c, "C", false));

        var result = _sut.Score([q], [new UserAnswerSubmissionViewModel
        {
            QuestionId = q.QuestionId.ToString(),
            AnswerIds = [a.ToString(), b.ToString(), c.ToString()]   // includes wrong c
        }]);

        Assert.False(result.UserAnswers[0].IsCorrect);
    }

    // ---- FillInTheBlanks / ShortAnswer ---------------------------------

    [Theory]
    [InlineData("paris", "paris")]
    [InlineData(" Paris ", "paris")]   // trimmed
    [InlineData("PARIS", "paris")]     // case-insensitive
    public void FillInTheBlanks_MatchVariants_Score(string userInput, string correctAnswer)
    {
        var q = TextQuestion(QuestionType.FillInTheBlanks, correctAnswer);

        var result = _sut.Score([q], [new UserAnswerSubmissionViewModel
        {
            QuestionId = q.QuestionId.ToString(),
            TextAnswer = userInput
        }]);

        Assert.True(result.UserAnswers[0].IsCorrect);
    }

    [Fact]
    public void FillInTheBlanks_WrongText_DoesNotScore()
    {
        var q = TextQuestion(QuestionType.FillInTheBlanks, "paris");

        var result = _sut.Score([q], [new UserAnswerSubmissionViewModel
        {
            QuestionId = q.QuestionId.ToString(),
            TextAnswer = "Lyon"
        }]);

        Assert.False(result.UserAnswers[0].IsCorrect);
    }

    [Fact]
    public void ShortAnswer_CaseInsensitiveMatch_Scores()
    {
        var q = TextQuestion(QuestionType.ShortAnswer, "Newton");

        var result = _sut.Score([q], [new UserAnswerSubmissionViewModel
        {
            QuestionId = q.QuestionId.ToString(),
            TextAnswer = "newton"
        }]);

        Assert.True(result.UserAnswers[0].IsCorrect);
    }

    // ---- LongAnswer ----------------------------------------------------

    [Fact]
    public void LongAnswer_StoredForReview_ExcludedFromDenominator()
    {
        var gradable = TextQuestion(QuestionType.ShortAnswer, "42");
        var longQ = LongAnswer();

        var result = _sut.Score(
            [gradable, longQ],
            [
                new UserAnswerSubmissionViewModel { QuestionId = gradable.QuestionId.ToString(), TextAnswer = "42" },
                new UserAnswerSubmissionViewModel { QuestionId = longQ.QuestionId.ToString(), TextAnswer = "essay" }
            ]);

        // Only 1 gradable question, LongAnswer excluded from denominator
        Assert.Equal(1, result.GradableCount);
        Assert.Equal(1, result.CorrectCount);
        Assert.Equal(100, result.Score);
        Assert.True(result.UserAnswers.First(ua => ua.QuestionId == longQ.QuestionId).NeedsReview);
    }

    [Fact]
    public void QuizOf4Gradable_Plus1Long_ScoresOutOf4()
    {
        var questions = new List<QuestionForScoring>
        {
            TextQuestion(QuestionType.ShortAnswer, "A"),
            TextQuestion(QuestionType.ShortAnswer, "B"),
            TextQuestion(QuestionType.ShortAnswer, "C"),
            TextQuestion(QuestionType.ShortAnswer, "D"),
            LongAnswer()
        };

        // Answer only 3 of 4 gradable correctly
        var submission = questions.Take(3).Select(q => new UserAnswerSubmissionViewModel
        {
            QuestionId = q.QuestionId.ToString(),
            TextAnswer = q.Answers.FirstOrDefault()?.Content
        }).Concat([new UserAnswerSubmissionViewModel
        {
            QuestionId = questions[^1].QuestionId.ToString(),
            TextAnswer = "long essay"
        }]).ToList();

        var result = _sut.Score(questions, submission);

        Assert.Equal(4, result.GradableCount);
        Assert.Equal(3, result.CorrectCount);
        Assert.Equal(75, result.Score);  // round(3/4*100) = 75
    }

    // ---- Edge cases ----------------------------------------------------

    [Fact]
    public void EmptySubmission_ScoresZeroWithoutThrowing()
    {
        var q1 = TextQuestion(QuestionType.ShortAnswer, "42");
        var q2 = SingleChoice((Guid.NewGuid(), "A", true));

        var result = _sut.Score([q1, q2], []);

        Assert.Equal(0, result.CorrectCount);
        Assert.Equal(0, result.Score);
        Assert.Equal(2, result.GradableCount);
    }

    [Fact]
    public void AllLongAnswer_GradableCountZero_ScoreIsZero()
    {
        var questions = new List<QuestionForScoring> { LongAnswer(), LongAnswer() };
        var submission = questions.Select(q => new UserAnswerSubmissionViewModel
        {
            QuestionId = q.QuestionId.ToString(),
            TextAnswer = "anything"
        }).ToList();

        var result = _sut.Score(questions, submission);

        Assert.Equal(0, result.GradableCount);
        Assert.Equal(0, result.Score);
    }
}
