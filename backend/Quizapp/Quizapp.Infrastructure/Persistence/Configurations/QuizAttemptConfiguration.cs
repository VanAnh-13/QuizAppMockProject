using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quizapp.Domain.Entities;

namespace Quizapp.Infrastructure.Persistence.Configurations;

internal sealed class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> entry)
    {
        entry.HasKey(attempt => attempt.Id);

        entry.Property(attempt => attempt.SubmitAt).IsConcurrencyToken();

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
