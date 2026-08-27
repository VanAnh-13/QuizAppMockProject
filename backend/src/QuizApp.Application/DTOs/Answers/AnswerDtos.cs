namespace QuizApp.Application.DTOs.Answers;

public class AnswerViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsActive { get; set; }
    public string QuestionId { get; set; } = string.Empty;
}

public class AnswerCreateViewModel
{
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsActive { get; set; } = true;
    public string QuestionId { get; set; } = string.Empty;
}

public class AnswerEditViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsActive { get; set; }
    public string QuestionId { get; set; } = string.Empty;
}
