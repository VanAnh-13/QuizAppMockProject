namespace Quizapp.Application.DTOs.UserManager;

public sealed class UpdateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserProfileDto Profile { get; set; } = new();
    public bool IsActive { get; set; }

    public sealed class Builder
    {
        private readonly UpdateUserDto _dto = new();

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

        public UpdateUserDto Build()
        {
            return (UpdateUserDto)_dto.MemberwiseClone();
        }
    }
}
