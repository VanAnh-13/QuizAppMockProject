using Quizapp.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;

namespace Quizapp.Infrastructure.Persistence.Configurations;

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> entry)
    {
        entry.HasKey(question => question.Id);

        entry.Property(question => question.Image)
            .HasMaxLength(FieldLimits.ImageUrlLength);

        entry.Property(question => question.Content)
            .HasMaxLength(FieldLimits.ContentLength)
            .IsRequired();

        var questionTypes = string.Join(", ", Enum.GetValues<QuestionType>()
            .Select(value => (int)value));

        entry.ToTable(table => table.HasCheckConstraint(
            "CK_Questions_QuestionType", $"[QuestionType] IN ({questionTypes})"));

        var levels = string.Join(", ", Enum.GetValues<QuestionLevel>()
            .Select(value => (int)value));

        entry.ToTable(table => table.HasCheckConstraint(
            "CK_Questions_Level", $"[Level] IS NULL OR [Level] IN ({levels})"));
    }
}
