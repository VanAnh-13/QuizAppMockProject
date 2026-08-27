using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizApp.Domain.Entities;
using QuizApp.Domain.Enums;

namespace QuizApp.Infrastructure.Persistence.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.Property(q => q.Content).HasMaxLength(2000).IsRequired();
        builder.Property(q => q.QuestionType).HasConversion<string>().HasMaxLength(32);
        builder.Property(q => q.IsActive).HasDefaultValue(true);

        builder.HasMany(q => q.Answers)
            .WithOne(a => a.Question)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.QuizQuestions)
            .WithOne(qq => qq.Question)
            .HasForeignKey(qq => qq.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(q => q.QuestionType);
    }
}
