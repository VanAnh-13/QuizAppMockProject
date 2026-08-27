using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizApp.Domain.Entities;

namespace QuizApp.Infrastructure.Persistence.Configurations;

public class QuizCodeConfiguration : IEntityTypeConfiguration<QuizCode>
{
    public void Configure(EntityTypeBuilder<QuizCode> builder)
    {
        builder.Property(c => c.Code).HasMaxLength(32).IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => new { c.QuizId, c.UserId });
    }
}
