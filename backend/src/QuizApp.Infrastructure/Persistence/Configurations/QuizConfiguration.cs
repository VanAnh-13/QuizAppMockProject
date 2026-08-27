using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizApp.Domain.Entities;

namespace QuizApp.Infrastructure.Persistence.Configurations;

public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder)
    {
        builder.Property(q => q.Title).HasMaxLength(256).IsRequired();
        builder.Property(q => q.Description).HasMaxLength(2000).IsRequired();
        builder.Property(q => q.ThumbnailUrl).HasMaxLength(512);
        builder.Property(q => q.IsActive).HasDefaultValue(false);

        builder.HasMany(q => q.QuizQuestions)
            .WithOne(qq => qq.Quiz)
            .HasForeignKey(qq => qq.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        // Attempt history must survive quiz deletion → quizzes are
        // soft-deactivated rather than hard-deleted when attempts exist.
        builder.HasMany(q => q.Attempts)
            .WithOne(a => a.Quiz)
            .HasForeignKey(a => a.QuizId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(q => q.Codes)
            .WithOne(c => c.Quiz)
            .HasForeignKey(c => c.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(q => q.Title);
        builder.HasIndex(q => q.IsActive);
    }
}
