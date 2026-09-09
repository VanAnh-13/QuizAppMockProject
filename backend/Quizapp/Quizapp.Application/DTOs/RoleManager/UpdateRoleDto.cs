namespace Quizapp.Application.DTOs.RoleManager;

public sealed class UpdateRoleDto
{
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public sealed class Builder
    {
        private readonly UpdateRoleDto _dto = new();

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

        public UpdateRoleDto Build()
        {
            return (UpdateRoleDto)_dto.MemberwiseClone();
        }
    }
}
