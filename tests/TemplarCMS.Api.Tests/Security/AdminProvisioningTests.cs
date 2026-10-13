using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using TemplarCMS.Abstractions.Security;
using TemplarCMS.Api.Bootstrap;
using TemplarCMS.Api.Security;
using TemplarCMS.Domain.Security;
using TemplarCMS.Persistence;
using Xunit;

namespace TemplarCMS.Api.Tests.Security;

public sealed class AdminProvisioningTests
{
    [Fact]
    public async Task SetupCreatesOneActiveAdminAndDistinctInstallationPasswords()
    {
        await using var first = new ProvisioningFixture();
        await using var other = new ProvisioningFixture();
        using var scope = first.Services.CreateScope();
        using var otherScope = other.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IAdminProvisioningService>();
        Assert.NotNull(service);
        var created = await service.SetupAsync(" ADMIN@example.test ", TestContext.Current.CancellationToken);
        var second = await otherScope.ServiceProvider.GetRequiredService<IAdminProvisioningService>().SetupAsync("admin@example.test", TestContext.Current.CancellationToken);
        Assert.Equal(AdminProvisioningStatus.Created, created.Status);
        Assert.NotEqual(created.TemporaryPassword, second.TemporaryPassword);
        var db = scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();
        var profile = await db.DirectoryUsers.SingleAsync(TestContext.Current.CancellationToken);
        var identity = await db.Users.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(profile.Id, identity.Id);
        Assert.Equal("Active", profile.Status);
        Assert.Equal("ADMIN@example.test", profile.Email);
        Assert.Equal("ADMIN@EXAMPLE.TEST", profile.NormalizedEmail);
        Assert.Equal("[\"TemplarAdmin\"]", profile.RolesJson);
        Assert.True(identity.MustChangePassword);
        Assert.Equal("TemplarAdmin", identity.UserName);
        Assert.NotEqual(created.TemporaryPassword, identity.PasswordHash);
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<TemplarCMS.Persistence.Security.CmsIdentityUser>>();
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(identity, identity.PasswordHash!, created.TemporaryPassword!));
        Assert.Single(await db.AdminProvisioning.ToListAsync(TestContext.Current.CancellationToken));
        var repeated = await service.SetupAsync("changed@example.test", TestContext.Current.CancellationToken);
        Assert.Equal(AdminProvisioningStatus.AlreadyInitialized, repeated.Status);
        Assert.Null(repeated.TemporaryPassword);
    }

    [Fact]
    public async Task ConcurrentSetupNeverCreatesTwoAccounts()
    {
        await using var fixture = new ProvisioningFixture();
        await fixture.Services.GetRequiredService<CmsRuntimeInitializer>().InitializeAsync(TestContext.Current.CancellationToken);
        using var first = fixture.Services.CreateScope();
        using var second = fixture.Services.CreateScope();
        Assert.NotNull(first.ServiceProvider.GetService<IAdminProvisioningService>());
        var outcomes = await Task.WhenAll(Task.Run(() => first.ServiceProvider.GetRequiredService<IAdminProvisioningService>().SetupAsync("admin@example.test", TestContext.Current.CancellationToken)), Task.Run(() => second.ServiceProvider.GetRequiredService<IAdminProvisioningService>().SetupAsync("admin@example.test", TestContext.Current.CancellationToken)));
        Assert.Single(outcomes, result => result.Status == AdminProvisioningStatus.Created);
        Assert.Single(outcomes, result => result.Status == AdminProvisioningStatus.AlreadyInitialized);
        using var check = fixture.Services.CreateScope();
        var db = check.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();
        Assert.Single(await db.Users.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InvalidOrCollidingEmailDoesNotAdoptPendingProfile()
    {
        await using var fixture = new ProvisioningFixture();
        await fixture.Services.GetRequiredService<CmsRuntimeInitializer>().InitializeAsync(TestContext.Current.CancellationToken);
        using var scope = fixture.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IAdminProvisioningService>();
        Assert.NotNull(service);
        Assert.Equal(AdminProvisioningStatus.InvalidEmail, (await service.SetupAsync("not an email", TestContext.Current.CancellationToken)).Status);
        var db = scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();
        db.DirectoryUsers.Add(new() { Id = Guid.NewGuid(), FirstName = "Pending", LastName = "Profile", Email = "admin@example.test", NormalizedEmail = "ADMIN@EXAMPLE.TEST", Revision = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AdminProvisioningStatus.DuplicateEmail, (await service.SetupAsync(" ADMIN@example.test ", TestContext.Current.CancellationToken)).Status);
        Assert.Empty(await db.Users.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Invited", (await db.DirectoryUsers.SingleAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Empty(await db.AdminProvisioning.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ResetPreservesAccountAndRevokesItsOldCredentials()
    {
        await using var fixture = new ProvisioningFixture();
        using var scope = fixture.Services.CreateScope();
        var service = scope.ServiceProvider.GetService<IAdminProvisioningService>();
        Assert.NotNull(service);
        var first = await service.SetupAsync("admin@example.test", TestContext.Current.CancellationToken);
        var db = scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();
        var account = await db.Users.SingleAsync(TestContext.Current.CancellationToken);
        var oldStamp = account.SecurityStamp;
        account.MustChangePassword = false;
        account.AccessFailedCount = 5;
        account.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var reset = await service.ResetAsync(TestContext.Current.CancellationToken);
        Assert.Equal(first.UserId, reset.UserId);
        Assert.NotEqual(first.TemporaryPassword, reset.TemporaryPassword);
        account = await db.Users.SingleAsync(TestContext.Current.CancellationToken);
        Assert.True(account.MustChangePassword);
        Assert.Null(account.LockoutEnd);
        Assert.Equal(0, account.AccessFailedCount);
        Assert.NotEqual(oldStamp, account.SecurityStamp);
        var profile = await db.DirectoryUsers.SingleAsync(TestContext.Current.CancellationToken);
        profile.Status = "Suspended";
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var inactive = await service.ResetAsync(TestContext.Current.CancellationToken);
        Assert.Equal(AdminProvisioningStatus.InactiveAccount, inactive.Status);
        Assert.Null(inactive.TemporaryPassword);
    }
}

internal sealed class ProvisioningFixture : IAsyncDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "TemplarCMS.AdminIdentity.Tests", Guid.NewGuid().ToString("N"));
    public ServiceProvider Services { get; }
    public ProvisioningFixture()
    {
        Directory.CreateDirectory(_root);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Persistence:Provider"] = "Sqlite", ["ConnectionStrings:TemplarCms"] = $"Data Source={Path.Combine(_root, "cms.db")};Default Timeout=2", ["Templates:TemplatesPath"] = Path.Combine(_root, "Templates"), ["AuthoringSecurity:Enabled"] = "true" }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddTemplarApiAuthoringSecurity(configuration);
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddTemplarCmsRuntime(configuration, new TestEnvironment(_root));
        Services = services.BuildServiceProvider();
        Services.GetRequiredService<CmsRuntimeInitializer>().InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
    }
    public async ValueTask DisposeAsync() { await Services.DisposeAsync(); Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(_root, true); }
    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "TemplarCMS.Api";
        public string WebRootPath { get; set; } = root;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
