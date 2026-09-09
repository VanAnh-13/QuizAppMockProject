namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class SaveQuizProgressDto : AttemptRevisionDto
{
    public List<SubmitAnswerDto> Answers { get; set; } = [];
}
