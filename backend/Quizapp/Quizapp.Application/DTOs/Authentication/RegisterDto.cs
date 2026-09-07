using Quizapp.Application.DTOs.UserManager;

namespace Quizapp.Application.DTOs.Authentication;

public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public UserProfileDto Profile { get; set; } = new();

    public sealed class Builder
    {
        private readonly RegisterDto _dto = new();

        public Builder WithUsername(string username)
        {
            ArgumentNullException.ThrowIfNull(username);
            _dto.Username = username;

            return this;
        }

        public Builder WithEmail(string email)
        {
            ArgumentNullException.ThrowIfNull(email);
            _dto.Email = email;

            return this;
        }

        public Builder WithPassword(string password)
        {
            ArgumentNullException.ThrowIfNull(password);
            _dto.Password = password;

            return this;
        }

        public Builder WithConfirmPassword(string confirmPassword)
        {
            ArgumentNullException.ThrowIfNull(confirmPassword);
            _dto.ConfirmPassword = confirmPassword;

            return this;
        }

        public Builder WithProfile(UserProfileDto profile)
        {
            ArgumentNullException.ThrowIfNull(profile);
            _dto.Profile = profile;

            return this;
        }

        public RegisterDto Build()
        {
            return (RegisterDto)_dto.MemberwiseClone();
        }
    }
}
