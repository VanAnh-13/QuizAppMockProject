using Quizapp.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quizapp.Domain.Entities;

namespace Quizapp.Infrastructure.Persistence.Configurations;

internal sealed class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> entry)
    {
        entry.HasKey(quiz => quiz.Id);
        entry.Property(quiz => quiz.Title).HasMaxLength(FieldLimits.QuizTitleLength).IsRequired();
        entry.Property(quiz => quiz.Description).HasMaxLength(FieldLimits.ContentLength);
        entry.Property(quiz => quiz.Image).HasMaxLength(FieldLimits.ImageUrlLength);
        entry.ToTable(table => table.HasCheckConstraint(
            "CK_Quizzes_PassedScore", $"[PassedScore] IS NULL OR [PassedScore] >= {Quiz.MinimumPassedScore}"));
        entry.ToTable(table => table.HasCheckConstraint("CK_Quizzes_PositiveDuration", "[Duration] > 0"));

        entry.HasMany(quiz => quiz.Questions)
            .WithMany(question => question.Quizzes)
            .UsingEntity<QuizQuestion>(
                assignment =>
                {
                    assignment.HasKey(value => value.Id);
                    assignment.HasIndex(value => new { value.QuizId, value.QuestionId }).IsUnique();
                    assignment.HasIndex(value => new { value.QuizId, value.Order });
                    assignment.ToTable(table => table.HasCheckConstraint(
                        "CK_QuizQuestions_PositiveOrder", $"[Order] >= {QuizQuestion.FirstOrder}"));
                });
    }
}
