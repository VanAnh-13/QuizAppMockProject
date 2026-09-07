using Quizapp.Application.DTOs.UserManager;

namespace Quizapp.Application.DTOs.Authentication;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto UserDto { get; set; } = new();

    public sealed class Builder
    {
        private readonly AuthResponseDto _dto = new();

        public Builder WithToken(string token)
        {
            ArgumentNullException.ThrowIfNull(token);
            _dto.Token = token;

            return this;
        }

        public Builder WithExpiresAt(DateTime expiresAt)
        {
            _dto.ExpiresAt = expiresAt;

            return this;
        }

        public Builder WithUserDto(UserDto userDto)
        {
            ArgumentNullException.ThrowIfNull(userDto);
            _dto.UserDto = userDto;

            return this;
        }

        public AuthResponseDto Build()
        {
            return (AuthResponseDto)_dto.MemberwiseClone();
        }
    }
}
