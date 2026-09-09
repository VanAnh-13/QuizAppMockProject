namespace Quizapp.Application.DTOs.Authentication;

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public sealed class Builder
    {
        private readonly LoginDto _dto = new();

        public Builder WithUsername(string username)
        {
            ArgumentNullException.ThrowIfNull(username);
            _dto.Username = username;

            return this;
        }

        public Builder WithPassword(string password)
        {
            ArgumentNullException.ThrowIfNull(password);
            _dto.Password = password;

            return this;
        }

        public LoginDto Build()
        {
            return (LoginDto)_dto.MemberwiseClone();
        }
    }
}
