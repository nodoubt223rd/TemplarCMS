using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TemplarCMS.Abstractions.Security;
using TemplarCMS.Api.Tests.Support;
using TemplarCMS.Persistence;
using Xunit;

namespace TemplarCMS.Api.Tests.Security;

public sealed class AdminAuthenticationTests
{
    [Fact]
    public async Task TemporaryLoginRequiresPasswordChangeAndInvalidatesOldSessions()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AdminIdentityApiFactory();
        using var first = await factory.CreateAdminClientAsync(false, ct);
        using var second = await factory.CreateAdminClientAsync(false, ct);
        var session = await first.GetFromJsonAsync<JsonElement>("/api/v1/auth/session", ct);
        Assert.True(session.GetProperty("mustChangePassword").GetBoolean());
        using var restricted = await first.PostAsJsonAsync("/api/v1/templates", new { name = "Must fail" }, ct);
        Assert.Equal(HttpStatusCode.Forbidden, restricted.StatusCode);
        using var change = await first.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = factory.TemporaryPassword, newPassword = "a fresh private passphrase ✓" }, ct);
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.False((await change.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("mustChangePassword").GetBoolean());
        using var old = await second.GetAsync("/api/v1/auth/session", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, old.StatusCode);
        using var fresh = factory.CreateAnonymousClient();
        await AdminIdentityApiFactory.RefreshCsrfAsync(fresh, ct);
        using var oldPassword = await fresh.PostAsJsonAsync("/api/v1/auth/login", new { loginName = "TemplarAdmin", password = factory.TemporaryPassword }, ct);
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
    }

    [Theory]
    [InlineData(14, false)]
    [InlineData(15, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public async Task PasswordBoundariesAreEnforced(int length, bool allowed)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AdminIdentityApiFactory();
        using var client = await factory.CreateAdminClientAsync(false, ct);
        using var change = await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = factory.TemporaryPassword, newPassword = new string('x', length) }, ct);
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.BadRequest, change.StatusCode);
    }

    [Fact]
    public async Task UnchangedAndWrongPasswordsRemainRestricted()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AdminIdentityApiFactory();
        using var client = await factory.CreateAdminClientAsync(false, ct);
        foreach (var request in new[] { new { currentPassword = factory.TemporaryPassword!, newPassword = factory.TemporaryPassword! }, new { currentPassword = "wrong password", newPassword = "a fresh private passphrase" } })
        {
            using var change = await client.PostAsJsonAsync("/api/v1/auth/change-password", request, ct);
            Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);
        }
        Assert.True((await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/session", ct)).GetProperty("mustChangePassword").GetBoolean());
    }

    [Fact]
    public async Task MutationsRequireAntiforgeryAndCookiesArePrivate()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AdminIdentityApiFactory();
        await factory.ProvisionAsync(ct);
        using var client = factory.CreateAnonymousClient();
        using var missing = await client.PostAsJsonAsync("/api/v1/auth/login", new { loginName = "TemplarAdmin", password = factory.TemporaryPassword }, ct);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        await AdminIdentityApiFactory.RefreshCsrfAsync(client, ct);
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { loginName = "TemplarAdmin", password = factory.TemporaryPassword }, ct);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"), value => value.StartsWith("__Host-TemplarAdmin="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expires=", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.True(login.Headers.CacheControl!.NoStore);
        Assert.DoesNotContain(factory.TemporaryPassword!, await login.Content.ReadAsStringAsync(ct));
    }

    [Fact]
    public async Task ResetAndInactiveProfilesImmediatelyRevokeSessions()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AdminIdentityApiFactory();
        using var client = await factory.CreateAdminClientAsync(true, ct);
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<IAdminProvisioningService>().ResetAsync(ct);
        using var resetRead = await client.GetAsync("/api/v1/auth/session", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, resetRead.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();
            var profile = await db.DirectoryUsers.SingleAsync(ct);
            profile.Status = "Suspended";
            await db.SaveChangesAsync(ct);
        }
        using var suspended = await client.GetAsync("/api/v1/auth/session", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, suspended.StatusCode);
    }

    [Fact]
    public async Task FailedPasswordSaveKeepsRestrictionAndLogoutIsSessionLocal()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AdminIdentityApiFactory();
        using var first = await factory.CreateAdminClientAsync(false, ct);
        using var second = await factory.CreateAdminClientAsync(false, ct);
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>().Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_password_change BEFORE UPDATE OF PasswordHash ON CmsIdentityUsers BEGIN SELECT RAISE(ABORT, 'Simulated save failure'); END;", ct);
        using var failed = await first.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = factory.TemporaryPassword, newPassword = "new passphrase must not save" }, ct);
        Assert.NotEqual(HttpStatusCode.OK, failed.StatusCode);
        Assert.True((await first.GetFromJsonAsync<JsonElement>("/api/v1/auth/session", ct)).GetProperty("mustChangePassword").GetBoolean());
        using var logout = await first.PostAsync("/api/v1/auth/logout", null, ct);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var loggedOut = await first.GetAsync("/api/v1/auth/session", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, loggedOut.StatusCode);
        using var unrelated = await second.GetAsync("/api/v1/auth/session", ct);
        Assert.Equal(HttpStatusCode.OK, unrelated.StatusCode);
    }
}
