using Quizapp.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quizapp.Domain.Entities;

namespace Quizapp.Infrastructure.Persistence.Configurations;

internal sealed class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> entry)
    {
        entry.HasKey(answer => answer.Id);
        entry.Property(answer => answer.Text).HasMaxLength(FieldLimits.ContentLength).IsRequired();
        entry.HasOne(answer => answer.QuestionNavigation)
            .WithMany(question => question.Answers)
            .HasForeignKey(answer => answer.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
