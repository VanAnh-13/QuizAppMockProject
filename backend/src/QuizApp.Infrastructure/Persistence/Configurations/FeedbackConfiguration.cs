using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuizApp.Domain.Entities;

namespace QuizApp.Infrastructure.Persistence.Configurations;

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.Property(f => f.Name).HasMaxLength(128).IsRequired();
        builder.Property(f => f.Email).HasMaxLength(256).IsRequired();
        builder.Property(f => f.Subject).HasMaxLength(256);
        builder.Property(f => f.Message).HasMaxLength(4000).IsRequired();
    }
}
