namespace QuizApp.Application.DTOs.Quizzes;

/// <summary>ViewModels from ANG.P.L001.Opt1.</summary>
public class QuizViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Duration { get; set; }
    public bool IsActive { get; set; }
    public string? ThumbnailUrl { get; set; }
}

public class QuizCreateViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Duration { get; set; }
    public bool IsActive { get; set; }
}

public class QuizEditViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Duration { get; set; }
    public bool IsActive { get; set; }
}

public class QuizQuestionCreateViewModel
{
    public string QuizId { get; set; } = string.Empty;
    public string QuestionId { get; set; } = string.Empty;
}

/// <summary>A question assigned to a quiz; carries the QuizQuestion assignment id for removal.</summary>
public class AssignedQuestionViewModel
{
    public string QuizQuestionId { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Domain.Enums.QuestionType QuestionType { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}
