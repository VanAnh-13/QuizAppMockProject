using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Domain.Entities;

namespace Quizapp.Application.Factories.QuizManager.Questions;

public interface IQuestionFactory
{
    Question Create(CreateQuestionDto request);
}
