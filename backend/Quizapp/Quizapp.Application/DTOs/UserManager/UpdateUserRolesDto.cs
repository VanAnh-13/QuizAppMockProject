namespace Quizapp.Application.DTOs.UserManager;

public sealed class UpdateUserRolesDto
{
    public List<Guid> RoleIds { get; set; } = [];

    public sealed class Builder
    {
        private readonly UpdateUserRolesDto _dto = new();

        public Builder WithRoleIds(IEnumerable<Guid> roleIds)
        {
            ArgumentNullException.ThrowIfNull(roleIds);
            _dto.RoleIds = [.. roleIds];

            return this;
        }

        public UpdateUserRolesDto Build()
        {
            var result = (UpdateUserRolesDto)_dto.MemberwiseClone();
            result.RoleIds = [.. _dto.RoleIds];

            return result;
        }
    }
}
