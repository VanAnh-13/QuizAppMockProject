namespace Quizapp.Application.DTOs.QuizHistory;

public class QuizAttemptDetailDto : QuizAttemptDto
{
    public IReadOnlyList<UserAnswerResultDto> Answers { get; set; } = [];
}
