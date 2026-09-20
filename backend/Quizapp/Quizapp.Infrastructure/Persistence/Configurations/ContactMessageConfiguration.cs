using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quizapp.Domain.Constants;
using Quizapp.Domain.Entities;

namespace Quizapp.Infrastructure.Persistence.Configurations;

internal sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> entry)
    {
        entry.HasKey(message => message.Id);
        entry.Property(message => message.FullName).HasMaxLength(FieldLimits.FullNameLength).IsRequired();
        entry.Property(message => message.Email).HasMaxLength(FieldLimits.EmailLength).IsRequired();
        entry.Property(message => message.Subject).HasMaxLength(FieldLimits.QuizTitleLength).IsRequired();
        entry.Property(message => message.Message).HasMaxLength(FieldLimits.ContentLength).IsRequired();
        entry.Property(message => message.NotificationErrorCode).HasMaxLength(FieldLimits.NameLength);
        entry.Property(message => message.ConfirmationErrorCode).HasMaxLength(FieldLimits.NameLength);
        entry.HasIndex(message => message.ReceivedAt);
        entry.ToTable(table =>
        {
            table.HasCheckConstraint("CK_ContactMessages_NotificationStatus", "[NotificationStatus] IN (0, 1, 2)");
            table.HasCheckConstraint("CK_ContactMessages_ConfirmationStatus", "[ConfirmationStatus] IN (0, 1, 2)");
        });
    }
}
