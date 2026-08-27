using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizApp.Domain.Entities;
using QuizApp.Domain.Enums;

namespace QuizApp.Infrastructure.Persistence.Configurations;

public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.Property(a => a.QuizCode).HasMaxLength(32);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(16);

        builder.HasMany(a => a.UserAnswers)
            .WithOne(ua => ua.QuizAttempt)
            .HasForeignKey(ua => ua.QuizAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.UserId, a.QuizId });
        builder.HasIndex(a => a.Status);
    }
}
