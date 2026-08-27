using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizApp.Domain.Entities;

namespace QuizApp.Infrastructure.Persistence.Configurations;

public class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.Property(a => a.Content).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.IsActive).HasDefaultValue(true);

        builder.HasIndex(a => a.QuestionId);
    }
}
