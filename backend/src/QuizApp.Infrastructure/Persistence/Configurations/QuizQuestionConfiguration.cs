using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizApp.Domain.Entities;

namespace QuizApp.Infrastructure.Persistence.Configurations;

public class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{
    public void Configure(EntityTypeBuilder<QuizQuestion> builder)
    {
        builder.Property(qq => qq.DisplayOrder).HasDefaultValue(0);

        // A question may only be assigned to a given quiz once.
        builder.HasIndex(qq => new { qq.QuizId, qq.QuestionId }).IsUnique();
    }
}
