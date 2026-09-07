namespace Quizapp.Application.DTOs.UserManager;

public sealed class UserProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Avatar { get; set; }

    public sealed class Builder
    {
        private readonly UserProfileDto _dto = new();

        public Builder WithFullName(string fullName)
        {
            ArgumentNullException.ThrowIfNull(fullName);
            _dto.FullName = fullName;

            return this;
        }

        public Builder WithPhoneNumber(string? phoneNumber)
        {
            _dto.PhoneNumber = phoneNumber;

            return this;
        }

        public Builder WithDateOfBirth(DateOnly? dateOfBirth)
        {
            _dto.DateOfBirth = dateOfBirth;

            return this;
        }

        public Builder WithAvatar(string? avatar)
        {
            _dto.Avatar = avatar;

            return this;
        }

        public UserProfileDto Build()
        {
            return (UserProfileDto)_dto.MemberwiseClone();
        }
    }
}
