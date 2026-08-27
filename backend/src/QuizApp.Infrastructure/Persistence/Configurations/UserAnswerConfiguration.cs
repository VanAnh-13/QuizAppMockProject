using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizApp.Domain.Entities;
using QuizApp.Domain.Enums;

namespace QuizApp.Infrastructure.Persistence.Configurations;

public class UserAnswerConfiguration : IEntityTypeConfiguration<UserAnswer>
{
    public void Configure(EntityTypeBuilder<UserAnswer> builder)
    {
        builder.Property(ua => ua.TextAnswer).HasMaxLength(4000);
        builder.Property(ua => ua.QuestionContent).HasMaxLength(2000).IsRequired();
        builder.Property(ua => ua.QuestionType).HasConversion<string>().HasMaxLength(32);
        builder.Property(ua => ua.AnswerContent).HasMaxLength(4000);
        builder.Property(ua => ua.CorrectAnswerContent).HasMaxLength(4000);

        // History survives question/answer deletion. ClientSetNull avoids SQL
        // Server's "multiple cascade paths" restriction: deleting a question or
        // answer nulls tracked UserAnswer references before the DELETE is sent.
        builder.HasOne(ua => ua.Question)
            .WithMany()
            .HasForeignKey(ua => ua.QuestionId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasOne(ua => ua.Answer)
            .WithMany()
            .HasForeignKey(ua => ua.AnswerId)
            .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasIndex(ua => ua.QuizAttemptId);
    }
}
