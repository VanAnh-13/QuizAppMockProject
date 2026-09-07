using Quizapp.Application.DTOs.RoleManager;

namespace Quizapp.Application.DTOs.UserManager;

public sealed class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Avatar { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IReadOnlyList<RoleDto> Roles { get; set; } = [];

    public sealed class Builder
    {
        private readonly UserDto _dto = new();

        public Builder WithId(Guid id)
        {
            _dto.Id = id;
            return this;
        }

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

        public Builder WithFullName(string? fullName)
        {
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

        public Builder WithIsActive(bool isActive)
        {
            _dto.IsActive = isActive;
            return this;
        }

        public Builder WithCreatedAt(DateTime createdAt)
        {
            _dto.CreatedAt = createdAt;
            return this;
        }

        public Builder WithUpdatedAt(DateTime updatedAt)
        {
            _dto.UpdatedAt = updatedAt;
            return this;
        }

        public Builder WithRoles(IEnumerable<RoleDto> roles)
        {
            ArgumentNullException.ThrowIfNull(roles);
            _dto.Roles = [.. roles];
            return this;
        }

        public UserDto Build()
        {
            var result = (UserDto)_dto.MemberwiseClone();
            result.Roles = [.. _dto.Roles];
            return result;
        }
    }
}
