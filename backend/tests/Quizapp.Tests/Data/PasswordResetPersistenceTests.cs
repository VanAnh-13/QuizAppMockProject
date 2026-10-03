using Microsoft.EntityFrameworkCore;
using Quizapp.Infrastructure.Persistence.Repositories;

namespace Quizapp.Tests.Data;

public class PasswordResetPersistenceTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    [SqlServerFact]
    public async Task Concurrent_resets_allow_only_one_password_change()
    {
        await using var setup = fixture.CreateContext();
        var user = TestEntities.User();
        user.IsActive = true;
        setup.Users.Add(user);
        await setup.SaveChangesAsync();
        var now = DateTimeOffset.UtcNow;
        var hash = new string('A', 64);
        var repository = new EfUserRepository(setup);
        Assert.True(await repository.TryIssuePasswordResetAsync(user.Id, user.SecurityStamp, hash, now,
            now.AddMinutes(15), now.AddSeconds(-60), CancellationToken.None));

        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        var attempts = await Task.WhenAll(
            new EfUserRepository(first).TryResetPasswordAsync(user.Id, user.SecurityStamp, hash,
                "first-password-hash", Guid.NewGuid(), now.AddSeconds(1), CancellationToken.None),
            new EfUserRepository(second).TryResetPasswordAsync(user.Id, user.SecurityStamp, hash,
                "second-password-hash", Guid.NewGuid(), now.AddSeconds(1), CancellationToken.None));
        Assert.Single(attempts, succeeded => succeeded);

        setup.ChangeTracker.Clear();
        var saved = await repository.GetByIdAsync(user.Id, CancellationToken.None);
        Assert.NotNull(saved);
        Assert.Equal(attempts[0] ? "first-password-hash" : "second-password-hash", saved.Password);
        Assert.NotEqual(user.SecurityStamp, saved.SecurityStamp);
        Assert.Null(saved.PasswordResetTokenHash);
        Assert.Null(saved.PasswordResetExpiresAt);
        Assert.Null(saved.PasswordResetSecurityStamp);
    }

    [SqlServerFact]
    public async Task Issuance_enforces_cooldown_and_new_link_invalidates_the_previous_link()
    {
        await using var db = fixture.CreateContext();
        var user = TestEntities.User();
        user.IsActive = true;
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var repository = new EfUserRepository(db);
        var now = DateTimeOffset.UtcNow;
        var firstHash = new string('A', 64);
        var nextHash = new string('B', 64);
        Assert.True(await repository.TryIssuePasswordResetAsync(user.Id, user.SecurityStamp, firstHash,
            now, now.AddMinutes(15), now.AddSeconds(-60), CancellationToken.None));
        Assert.False(await repository.TryIssuePasswordResetAsync(user.Id, user.SecurityStamp, nextHash,
            now.AddSeconds(59), now.AddMinutes(16), now.AddSeconds(-1), CancellationToken.None));
        Assert.True(await repository.TryIssuePasswordResetAsync(user.Id, user.SecurityStamp, nextHash,
            now.AddSeconds(60), now.AddMinutes(16), now, CancellationToken.None));
        Assert.False(await repository.TryResetPasswordAsync(user.Id, user.SecurityStamp, firstHash,
            "password-hash", Guid.NewGuid(), now.AddSeconds(61), CancellationToken.None));
        Assert.True(await repository.TryResetPasswordAsync(user.Id, user.SecurityStamp, nextHash,
            "password-hash", Guid.NewGuid(), now.AddSeconds(61), CancellationToken.None));
    }

    [SqlServerTheory]
    [InlineData("expired")]
    [InlineData("stamp")]
    [InlineData("inactive")]
    public async Task Ineligible_links_cannot_change_password_at_the_database_boundary(string reason)
    {
        await using var db = fixture.CreateContext();
        var user = TestEntities.User();
        user.IsActive = true;
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var repository = new EfUserRepository(db);
        var now = DateTimeOffset.UtcNow;
        var stamp = user.SecurityStamp;
        var hash = new string('A', 64);
        Assert.True(await repository.TryIssuePasswordResetAsync(user.Id, stamp, hash,
            now, now.AddMinutes(15), now.AddSeconds(-60), CancellationToken.None));
        if (reason == "stamp") user.SecurityStamp = Guid.NewGuid();
        if (reason == "inactive") user.IsActive = false;
        await db.SaveChangesAsync();

        Assert.False(await repository.TryResetPasswordAsync(user.Id, stamp, hash,
            "must-not-be-saved", Guid.NewGuid(), reason == "expired" ? now.AddMinutes(15) : now, CancellationToken.None));
        db.ChangeTracker.Clear();
        Assert.Equal(user.Password, (await repository.GetByIdAsync(user.Id, CancellationToken.None))!.Password);
    }
}
