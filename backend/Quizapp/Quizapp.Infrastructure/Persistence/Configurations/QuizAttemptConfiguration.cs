using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quizapp.Domain.Entities;

namespace Quizapp.Infrastructure.Persistence.Configurations;

internal sealed class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> entry)
    {
        entry.HasKey(attempt => attempt.Id);

        entry.Property(attempt => attempt.SubmitAt).IsConcurrencyToken();
        entry.Property(attempt => attempt.Revision).IsConcurrencyToken();

        var utcTimestamp = new ValueConverter<DateTime, DateTime>(
            value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        entry.Property(attempt => attempt.StartedAt).HasConversion(utcTimestamp);
        entry.Property(attempt => attempt.ExpiresAt).HasConversion(utcTimestamp);
        entry.Property(attempt => attempt.PausedAt).HasConversion(utcTimestamp);
        entry.Property(attempt => attempt.LastSavedAt).HasConversion(utcTimestamp);
        entry.Property(attempt => attempt.SubmitAt).HasConversion(utcTimestamp);

        entry.HasOne(attempt => attempt.QuizNavigation)
            .WithMany(quiz => quiz.QuizAttempts)
            .HasForeignKey(attempt => attempt.QuizId)
            .OnDelete(DeleteBehavior.NoAction);

        entry.HasOne(attempt => attempt.UserNavigation)
            .WithMany(user => user.QuizAttempts)
            .HasForeignKey(attempt => attempt.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
