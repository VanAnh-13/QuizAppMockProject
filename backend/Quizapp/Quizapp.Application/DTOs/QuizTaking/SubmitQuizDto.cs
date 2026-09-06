namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class SubmitQuizDto
{
    public List<SubmitAnswerDto> Answers { get; init; } = [];
}
