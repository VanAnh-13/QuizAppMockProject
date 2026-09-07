namespace Quizapp.Application.Abstractions.Authentication;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string hash, string password);
}
