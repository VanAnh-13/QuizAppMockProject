using QuizApp.Application.DTOs.QuizCodes;
using QuizApp.Application.Interfaces;
using QuizApp.Domain.Entities;
using QuizApp.Domain.Enums;

namespace QuizApp.Application.Services;

/// <summary>
/// Grades a quiz submission using per-question-type rules.
/// Pure logic — no database access, no HTTP, easily unit-tested.
/// </summary>
public class ScoringService : IScoringService
{
    public ScoringResult Score(
        IReadOnlyList<QuestionForScoring> questions,
        IReadOnlyList<UserAnswerSubmissionViewModel> submission)
    {
        // Index submission by questionId for O(1) lookup.
        var submittedByQuestion = submission
            .GroupBy(s => s.QuestionId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var userAnswers = new List<UserAnswer>(questions.Count);
        int correctCount = 0;
        int gradableCount = 0;

        foreach (var question in questions)
        {
            submittedByQuestion.TryGetValue(question.QuestionId.ToString(), out var submitted);
            var ua = GradeQuestion(question, submitted);
            userAnswers.Add(ua);

            if (question.QuestionType != QuestionType.LongAnswer)
            {
                gradableCount++;
                if (ua.IsCorrect) correctCount++;
            }
        }

        int score = gradableCount == 0
            ? 0
            : (int)Math.Round(correctCount / (double)gradableCount * 100);

        return new ScoringResult
        {
            UserAnswers = userAnswers,
            CorrectCount = correctCount,
            GradableCount = gradableCount,
            Score = score
        };
    }

    private static UserAnswer GradeQuestion(
        QuestionForScoring question,
        UserAnswerSubmissionViewModel? submitted)
    {
        var ua = new UserAnswer
        {
            QuestionId = question.QuestionId,
            QuestionContent = question.QuestionContent,
            QuestionType = question.QuestionType
        };

        switch (question.QuestionType)
        {
            case QuestionType.SingleChoice:
            case QuestionType.TrueFalse:
                GradeSingleChoice(ua, question, submitted);
                break;

            case QuestionType.MultipleChoice:
                GradeMultipleChoice(ua, question, submitted);
                break;

            case QuestionType.FillInTheBlanks:
            case QuestionType.ShortAnswer:
                GradeTextAnswer(ua, question, submitted);
                break;

            case QuestionType.LongAnswer:
                GradeLongAnswer(ua, submitted);
                break;
        }

        return ua;
    }

    private static void GradeSingleChoice(
        UserAnswer ua,
        QuestionForScoring question,
        UserAnswerSubmissionViewModel? submitted)
    {
        var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect);
        ua.CorrectAnswerContent = correctAnswer?.Content;

        if (submitted?.AnswerId == null || !Guid.TryParse(submitted.AnswerId, out var answerId))
        {
            ua.IsCorrect = false;
            return;
        }

        ua.AnswerId = answerId;
        var chosenAnswer = question.Answers.FirstOrDefault(a => a.AnswerId == answerId);
        ua.AnswerContent = chosenAnswer?.Content;
        ua.IsCorrect = chosenAnswer?.IsCorrect == true;
    }

    private static void GradeMultipleChoice(
        UserAnswer ua,
        QuestionForScoring question,
        UserAnswerSubmissionViewModel? submitted)
    {
        var correctIds = question.Answers
            .Where(a => a.IsCorrect)
            .Select(a => a.AnswerId)
            .ToHashSet();

        ua.CorrectAnswerContent = string.Join(", ",
            question.Answers.Where(a => a.IsCorrect).Select(a => a.Content));

        // Collect submitted ids from both answerId and answerIds
        var submittedIds = new HashSet<Guid>();
        if (submitted?.AnswerId != null && Guid.TryParse(submitted.AnswerId, out var single))
            submittedIds.Add(single);
        if (submitted?.AnswerIds != null)
        {
            foreach (var idStr in submitted.AnswerIds)
            {
                if (Guid.TryParse(idStr, out var gid)) submittedIds.Add(gid);
            }
        }

        ua.AnswerContent = string.Join(", ",
            question.Answers.Where(a => submittedIds.Contains(a.AnswerId)).Select(a => a.Content));

        // All-or-nothing: submitted set must equal the correct set exactly.
        ua.IsCorrect = submittedIds.SetEquals(correctIds);
    }

    private static void GradeTextAnswer(
        UserAnswer ua,
        QuestionForScoring question,
        UserAnswerSubmissionViewModel? submitted)
    {
        // Any correct answer marked isCorrect in the answer bank is a valid match.
        var correctTexts = question.Answers
            .Where(a => a.IsCorrect)
            .Select(a => a.Content.Trim())
            .ToList();

        ua.CorrectAnswerContent = string.Join(" / ", correctTexts);
        ua.TextAnswer = submitted?.TextAnswer;
        ua.AnswerContent = submitted?.TextAnswer;

        var userText = submitted?.TextAnswer?.Trim() ?? string.Empty;
        ua.IsCorrect = correctTexts.Any(c =>
            string.Equals(c, userText, StringComparison.OrdinalIgnoreCase));
    }

    private static void GradeLongAnswer(UserAnswer ua, UserAnswerSubmissionViewModel? submitted)
    {
        // Long-answer is stored for manual review, excluded from the grading denominator.
        ua.TextAnswer = submitted?.TextAnswer;
        ua.AnswerContent = submitted?.TextAnswer;
        ua.IsCorrect = false;
        ua.NeedsReview = true;
    }
}
