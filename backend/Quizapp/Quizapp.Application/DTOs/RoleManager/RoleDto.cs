namespace Quizapp.Application.DTOs.RoleManager;

public sealed class RoleDto
{
    public Guid Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public sealed class Builder
    {
        private readonly RoleDto _dto = new();

        public Builder WithId(Guid id)
        {
            _dto.Id = id;

            return this;
        }

        public Builder WithRoleName(string roleName)
        {
            ArgumentNullException.ThrowIfNull(roleName);
            _dto.RoleName = roleName;

            return this;
        }

        public Builder WithDescription(string? description)
        {
            _dto.Description = description;

            return this;
        }

        public Builder WithIsActive(bool isActive)
        {
            _dto.IsActive = isActive;

            return this;
        }

        public RoleDto Build()
        {
            return (RoleDto)_dto.MemberwiseClone();
        }
    }
}
