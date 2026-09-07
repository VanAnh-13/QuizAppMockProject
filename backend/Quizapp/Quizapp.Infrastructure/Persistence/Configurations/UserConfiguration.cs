using Quizapp.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;

namespace Quizapp.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entry)
    {
        entry.HasKey(user => user.Id);

        entry.Property(user => user.FullName)
            .HasMaxLength(FieldLimits.FullNameLength);

        entry.Property(user => user.PhoneNumber)
            .HasMaxLength(FieldLimits.PhoneNumberLength);

        entry.Property(user => user.DateOfBirth)
            .HasColumnType("date");

        entry.Property(user => user.Avatar)
            .HasMaxLength(FieldLimits.ImageUrlLength);

        entry.Property(user => user.IsActive)
            .HasComputedColumnSql(
                $"CONVERT(bit, CASE WHEN [Status] = {(int)UserStatus.Active} THEN 1 ELSE 0 END)", stored: true);

        entry.Property(user => user.Username)
            .HasMaxLength(FieldLimits.NameLength)
            .IsRequired();

        entry.Property(user => user.Email)
            .HasMaxLength(FieldLimits.EmailLength)
            .IsRequired();

        entry.Property(user => user.Password)
            .HasMaxLength(FieldLimits.PasswordLength)
            .IsRequired();

        entry.HasIndex(user => user.Username)
            .IsUnique();

        entry.HasIndex(user => user.Email)
            .IsUnique();

        entry.HasMany(user => user.Roles)
            .WithMany(role => role.Users)
            .UsingEntity<UserRole>(assignment =>
                assignment.HasKey(value => new { value.UserId, value.RoleId }));
    }
}
