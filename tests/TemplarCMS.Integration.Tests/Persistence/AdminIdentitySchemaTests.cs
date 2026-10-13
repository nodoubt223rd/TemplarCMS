using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TemplarCMS.Persistence;
using TemplarCMS.Persistence.Content;
using TemplarCMS.Persistence.Security;
using Xunit;

namespace TemplarCMS.Integration.Tests.Persistence;

public sealed class AdminIdentitySchemaTests
{
    [Fact]
    public async Task FreshSchemaStoresIdentityAndOneMarker()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var entity = db.Model.FindEntityType("TemplarCMS.Persistence.Security.CmsIdentityUser");
        Assert.NotNull(entity);
        Assert.Equal("CmsIdentityUsers", entity.GetTableName());
        Assert.NotNull(entity.FindProperty("MustChangePassword"));
        Assert.Equal(254, entity.FindProperty("Email")!.GetMaxLength());
        Assert.Null(db.Model.FindEntityType(typeof(DirectoryUserRow))!.FindProperty("PasswordHash"));
        Assert.NotNull(db.Model.FindEntityType("TemplarCMS.Persistence.Security.AdminProvisioningRow"));
    }

    [Fact]
    public async Task LegacySqlitePreservesDirectoryAndContent()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var profile = Profile();
        db.DirectoryUsers.Add(profile);
        db.ContentItems.Add(new PersistenceContentItem { Id = Guid.NewGuid(), Name = "Preserved", Key = "preserved", TemplateId = Guid.NewGuid() });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        // An existing installation has these CMS tables but no credential tables.
        await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS AdminProvisioning; DROP TABLE IF EXISTS CmsIdentityUserTokens; DROP TABLE IF EXISTS CmsIdentityUserLogins; DROP TABLE IF EXISTS CmsIdentityUserClaims; DROP TABLE IF EXISTS CmsIdentityUsers;", TestContext.Current.CancellationToken);
        await IdentitySchemaInitializer.EnsureAsync(db, TestContext.Current.CancellationToken);
        await IdentitySchemaInitializer.EnsureAsync(db, TestContext.Current.CancellationToken);
        Assert.Equal(profile.Email, (await db.DirectoryUsers.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).Email);
        Assert.Equal("Preserved", (await db.ContentItems.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).Name);
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM CmsIdentityUsers").SingleAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MarkerCannotDuplicateOrReferenceMissingUser()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var db = CreateDb(connection);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(db.Model.FindEntityType("TemplarCMS.Persistence.Security.AdminProvisioningRow"));
        var profile = Profile();
        db.DirectoryUsers.Add(profile);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO CmsIdentityUsers (Id, UserName, NormalizedUserName, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, MustChangePassword) VALUES ({profile.Id}, 'TemplarAdmin', 'TEMPLARADMIN', 0, 0, 0, 1, 0, 1)", TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AdminProvisioning (Id, UserId, CreatedAt) VALUES (1, {Guid.NewGuid()}, {DateTimeOffset.UtcNow})", TestContext.Current.CancellationToken));
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AdminProvisioning (Id, UserId, CreatedAt) VALUES (1, {profile.Id}, {DateTimeOffset.UtcNow})", TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AdminProvisioning (Id, UserId, CreatedAt) VALUES (2, {profile.Id}, {DateTimeOffset.UtcNow})", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO AdminProvisioning (Id, UserId, CreatedAt) VALUES (1, {Guid.NewGuid()}, {DateTimeOffset.UtcNow})", TestContext.Current.CancellationToken));
        db.DirectoryUsers.Remove(profile);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void SqlServerModelRequiresIdentitySchema()
    {
        using var db = new TemplarCmsDbContext(new DbContextOptionsBuilder<TemplarCmsDbContext>().UseSqlServer("Server=unused;Database=model").Options);
        var identity = db.Model.FindEntityType("TemplarCMS.Persistence.Security.CmsIdentityUser");
        Assert.NotNull(identity);
        Assert.Equal("nvarchar(254)", identity.FindProperty("Email")!.GetColumnType());
        var marker = db.Model.FindEntityType("TemplarCMS.Persistence.Security.AdminProvisioningRow");
        Assert.NotNull(marker);
        Assert.Contains(marker.GetForeignKeys(), key => key.DeleteBehavior == DeleteBehavior.Restrict);
    }

    private static TemplarCmsDbContext CreateDb(SqliteConnection connection) => new(new DbContextOptionsBuilder<TemplarCmsDbContext>().UseSqlite(connection).Options);
    private static DirectoryUserRow Profile() => new() { Id = Guid.NewGuid(), FirstName = "Existing", LastName = "Profile", Email = "existing@example.test", NormalizedEmail = "EXISTING@EXAMPLE.TEST", CreatedAt = DateTimeOffset.UtcNow, Revision = Guid.NewGuid() };
}
