using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Quizapp.Infrastructure.Persistence;
using Quizapp.Domain.Entities;

namespace Quizapp.Tests.Data;

public class QuizAppModelTests
{
    [Fact]
    public void Attempts_track_submission_concurrency_and_require_start_and_expiry_times()
    {
        using var context = CreateContext();
        var attempt = context.Model.FindEntityType(typeof(QuizAttempt))!;
        var submittedAt = attempt.FindProperty(nameof(QuizAttempt.SubmitAt))!;
        Assert.True(submittedAt.IsNullable);
        Assert.True(submittedAt.IsConcurrencyToken);
        Assert.True(attempt.FindProperty(nameof(QuizAttempt.Revision))!.IsConcurrencyToken);
        Assert.True(attempt.FindProperty(nameof(QuizAttempt.PausedAt))!.IsNullable);
        Assert.False(attempt.FindProperty(nameof(QuizAttempt.StartedAt))!.IsNullable);
        Assert.False(attempt.FindProperty(nameof(QuizAttempt.ExpiresAt))!.IsNullable);
    }

    [Fact]
    public void Model_builds_without_accidental_shadow_foreign_keys()
    {
        using var context = CreateContext();

        var foreignKeys = context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys());

        Assert.All(foreignKeys, key =>
            Assert.All(key.Properties, property => Assert.False(property.IsShadowProperty(), property.Name)));

        Assert.Null(context.Model.FindEntityType(typeof(Question))!.FindProperty("QuizId"));
        Assert.Null(context.Model.FindEntityType(typeof(User))!.FindProperty("RoleId"));
    }

    [Fact]
    public void Sql_server_schema_can_be_generated()
    {
        using var context = CreateContext();

        var script = context.Database.GenerateCreateScript();

        Assert.Contains("CREATE TABLE [QuizQuestions]", script);
        Assert.Contains("CREATE TABLE [UserRoles]", script);
    }

    [Fact]
    public void User_email_is_required_limited_and_unique()
    {
        using var context = CreateContext();
        var userType = context.Model.FindEntityType(typeof(User))!;
        var email = userType.FindProperty(nameof(User.Email))!;

        Assert.False(email.IsNullable);
        Assert.Equal(256, email.GetMaxLength());

        Assert.Contains(userType.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(User.Email)]));
    }

    [Fact]
    public void Cascade_graph_has_no_cycles_or_multiple_paths_to_a_table()
    {
        using var context = CreateContext();

        var entities = context.Model.GetEntityTypes()
            .ToArray();

        foreach (var root in entities)
        {
            var visited = new HashSet<IEntityType> { root };
            var pending = new Queue<IEntityType>();
            pending.Enqueue(root);

            while (pending.TryDequeue(out var parent))
            {
                var currentParent = parent;

                var children = entities.SelectMany(entity => entity.GetForeignKeys())
                    .Where(key =>
                        key.PrincipalEntityType == currentParent && key.DeleteBehavior == DeleteBehavior.Cascade)
                    .Select(key => key.DeclaringEntityType);

                foreach (var child in children)
                {
                    Assert.True(visited.Add(child), $"Cascade from {root.Name} reaches {child.Name} more than once.");
                    pending.Enqueue(child);
                }
            }
        }
    }

    [Fact]
    public void Moving_persistence_preserves_migration_history_and_model_snapshot()
    {
        using var context = CreateContext();

        Assert.Equal(
            ["20260905114419_Initial", "20260905153421_AddUserEmail"],
            context.Database.GetMigrations()
                .Take(2));

        Assert.False(context.Database.HasPendingModelChanges());
    }

    private static QuizAppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<QuizAppDbContext>().UseSqlServer()
            .Options);
}
