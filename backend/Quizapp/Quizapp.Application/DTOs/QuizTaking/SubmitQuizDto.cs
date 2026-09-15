namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class SubmitQuizDto
{
    public Guid AttemptId { get; set; }
    public long? Revision { get; set; }
    public List<SubmitAnswerDto> Answers { get; set; } = [];
}
