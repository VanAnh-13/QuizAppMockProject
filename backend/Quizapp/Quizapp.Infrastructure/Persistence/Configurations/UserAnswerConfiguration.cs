using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quizapp.Domain.Entities;

namespace Quizapp.Infrastructure.Persistence.Configurations;

internal sealed class UserAnswerConfiguration : IEntityTypeConfiguration<UserAnswer>
{
    public void Configure(EntityTypeBuilder<UserAnswer> entry)
    {
        entry.HasKey(userAnswer => userAnswer.Id);

        entry.HasOne(userAnswer => userAnswer.QuizAttemptNavigation)
            .WithMany(attempt => attempt.UserAnswers)
            .HasForeignKey(userAnswer => userAnswer.QuizAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        entry.HasOne(userAnswer => userAnswer.QuestionNavigation)
            .WithMany(question => question.UserAnswers)
            .HasForeignKey(userAnswer => userAnswer.QuestionId)
            .OnDelete(DeleteBehavior.NoAction);

        entry.HasOne(userAnswer => userAnswer.AnswerNavigation)
            .WithMany(answer => answer.UserAnswers)
            .HasForeignKey(userAnswer => new { userAnswer.AnswerId, userAnswer.QuestionId })
            .HasPrincipalKey(answer => new { answer.Id, answer.QuestionId })
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        entry.HasIndex(userAnswer => new { userAnswer.QuizAttemptId, userAnswer.QuestionId, userAnswer.AnswerId })
            .IsUnique()
            .HasFilter("[AnswerId] IS NOT NULL");

        entry.HasIndex(userAnswer => new { userAnswer.QuizAttemptId, userAnswer.QuestionId })
            .IsUnique()
            .HasFilter("[AnswerId] IS NULL");

        entry.ToTable(table => table.HasCheckConstraint("CK_UserAnswers_Response", """
            ([AnswerId] IS NOT NULL AND [ResponseText] IS NULL)
            OR ([AnswerId] IS NULL AND [ResponseText] IS NOT NULL
                AND LEN(LTRIM(RTRIM([ResponseText]))) > 0)
            """));
    }
}