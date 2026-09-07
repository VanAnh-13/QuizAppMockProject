namespace Quizapp.Application.DTOs.UserManager;

public sealed class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public UserProfileDto Profile { get; set; } = new();
    public bool IsActive { get; set; }

    public sealed class Builder
    {
        private readonly CreateUserDto _dto = new();

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

        public Builder WithIsActive(bool isActive)
        {
            _dto.IsActive = isActive;
            return this;
        }

        public CreateUserDto Build()
        {
            return (CreateUserDto)_dto.MemberwiseClone();
        }
    }
}
