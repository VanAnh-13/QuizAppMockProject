using QuizApp.Domain.Enums;

namespace QuizApp.Application.DTOs.Questions;

public class QuestionViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public bool IsActive { get; set; }
}

public class QuestionCreateViewModel
{
    public string Content { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public bool IsActive { get; set; } = true;
}

public class QuestionEditViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public bool IsActive { get; set; }
}
