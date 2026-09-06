using Quizapp.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quizapp.Domain.Entities;

namespace Quizapp.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> entry)
    {
        entry.HasKey(role => role.Id);
        entry.Property(role => role.RoleName).HasMaxLength(FieldLimits.NameLength).IsRequired();
        entry.Property(role => role.Description).HasMaxLength(FieldLimits.ContentLength);
        entry.HasIndex(role => role.RoleName).IsUnique();
    }
}
