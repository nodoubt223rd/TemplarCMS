using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TemplarCMS.Domain.Security;
using TemplarCMS.Persistence;
using TemplarCMS.Persistence.Security;
using Xunit;

namespace TemplarCMS.Integration.Tests.Persistence;

public sealed class UserDirectoryRepositoryTests
{
    [Fact]
    public async Task FreshSqliteDirectoryRoundTripsCatalogAndEnforcesAssignmentTransitions()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var db = new TemplarCmsDbContext(new DbContextOptionsBuilder<TemplarCmsDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        Assert.Empty(await db.DirectoryUsers.ToArrayAsync(TestContext.Current.CancellationToken));
        await VerifyContractAsync(db);
    }

    [Fact]
    public async Task SqlServerDirectoryRoundTripsCatalogInsideRolledBackTransaction()
    {
        var connection = Environment.GetEnvironmentVariable("TEMPLARCMS_SQL_REHEARSAL_CONNECTION");
        if (string.IsNullOrEmpty(connection)) Assert.Skip("Set TEMPLARCMS_SQL_REHEARSAL_CONNECTION for the opt-in SQL test.");
        Assert.StartsWith("templarcms_rehearsal_", new SqlConnectionStringBuilder(connection).InitialCatalog);
        await using var db = new TemplarCmsDbContext(new DbContextOptionsBuilder<TemplarCmsDbContext>().UseSqlServer(connection).Options);
        await using var transaction = await db.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        try { await VerifyContractAsync(db); }
        finally { await transaction.RollbackAsync(CancellationToken.None); }
    }

    private static async Task VerifyContractAsync(TemplarCmsDbContext db)
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = new EfUserDirectoryRepository(db);
        var profile = new DirectoryUserProfile("Directory", "Test", $"{Guid.NewGuid():N}@example.test", "en", []);
        foreach (var invalid in new[] {
            new[] { DirectoryRole.MarketingAutomationEditors },
            new[] { (DirectoryRole)99 },
            new[] { DirectoryRole.Author, DirectoryRole.Author } })
        {
            Assert.Equal(DirectoryWriteStatus.InvalidRoles, (await repository.CreateAsync(profile with { Roles = invalid }, ct)).Status);
        }
        var created = await repository.CreateAsync(profile, ct);
        Assert.Equal(DirectoryWriteStatus.Saved, created.Status);
        var user = created.User!;
        db.ChangeTracker.Clear();
        Assert.Empty((await repository.GetAsync(user.Id, ct))!.Roles);
        var all = profile with { Roles = [DirectoryRole.Author, DirectoryRole.Developer, DirectoryRole.FormsEditor, DirectoryRole.TemplarAdmin] };
        var updated = await repository.UpdateAsync(user.Id, user.Revision, all, ct);
        Assert.Equal(DirectoryWriteStatus.Saved, updated.Status);
        var row = await db.DirectoryUsers.SingleAsync(row => row.Id == user.Id, ct);
        Assert.Equal(new[] { "Author", "Developer", "FormsEditor", "TemplarAdmin" }, JsonSerializer.Deserialize<string[]>(row.RolesJson));
        row.RolesJson = "[\"Author\",\"MarketingAutomationEditors\"]";
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var current = (await repository.GetAsync(user.Id, ct))!;
        var preserved = await repository.UpdateAsync(user.Id, current.Revision, profile with { Roles = current.Roles }, ct);
        Assert.Equal(DirectoryWriteStatus.Saved, preserved.Status);
        var removed = await repository.UpdateAsync(user.Id, preserved.User!.Revision, profile, ct);
        Assert.Equal(DirectoryWriteStatus.Saved, removed.Status);
        var planned = profile with { Roles = [DirectoryRole.MarketingAutomationEditors] };
        Assert.Equal(DirectoryWriteStatus.Conflict, (await repository.UpdateAsync(user.Id, preserved.User.Revision, planned, ct)).Status);
        Assert.Equal(DirectoryWriteStatus.InvalidRoles, (await repository.UpdateAsync(user.Id, removed.User!.Revision, planned, ct)).Status);
        db.ChangeTracker.Clear();
        Assert.Empty((await repository.GetAsync(user.Id, ct))!.Roles);
    }
}
