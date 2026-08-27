using QuizApp.Application.Common.Paging;
using QuizApp.Application.DTOs.Answers;
using QuizApp.Application.DTOs.Questions;

namespace QuizApp.Application.Interfaces;

/// <summary>Backend mirror of the Angular IQuestionService contract.</summary>
public interface IQuestionService
{
    Task<PagedResult<QuestionViewModel>> GetPagedAsync(PagedQuery query, CancellationToken ct = default);

    Task<QuestionViewModel> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<QuestionViewModel> CreateAsync(QuestionCreateViewModel model, CancellationToken ct = default);

    Task<QuestionViewModel> UpdateAsync(QuestionEditViewModel model, CancellationToken ct = default);

    /// <summary>
    /// Deletes the question. When still assigned to quizzes the caller must confirm;
    /// on confirm the question is removed from those quizzes and attempt history survives.
    /// </summary>
    Task<QuestionDeletePreview> GetDeletePreviewAsync(Guid id, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IAnswerService
{
    Task<IReadOnlyList<AnswerViewModel>> GetAnswersByQuestionIdAsync(Guid questionId, CancellationToken ct = default);

    Task<AnswerViewModel> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<AnswerViewModel> CreateAsync(AnswerCreateViewModel model, CancellationToken ct = default);

    Task<AnswerViewModel> UpdateAsync(AnswerEditViewModel model, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class QuestionDeletePreview
{
    public bool IsAssignedToQuizzes { get; init; }
    public string[] QuizTitles { get; init; } = Array.Empty<string>();
    public string Warning { get; init; } = string.Empty;
}
