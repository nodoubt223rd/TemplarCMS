using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TemplarCMS.Api.Tests.Support;
using TemplarCMS.Persistence;
using Xunit;

namespace TemplarCMS.Api.Tests.Security;

public sealed class AdminAuthorizationTests
{
    [Theory]
    [InlineData("/api/v1/templates")]
    [InlineData("/api/v1/template-organization")]
    [InlineData("/api/v1/field-types")]
    [InlineData("/api/v1/content/root/children")]
    [InlineData("/api/v1/content/root/branch")]
    [InlineData("/api/v1/content/workspaces/content/branch")]
    [InlineData("/api/v1/media/assets")]
    [InlineData("/api/v1/security/users")]
    public async Task EveryWorkspaceReadRequiresUnrestrictedAdmin(string route)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AdminIdentityApiFactory();
        using var anonymous = factory.CreateAnonymousClient();
        using var missing = await anonymous.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        using var restricted = await factory.CreateAdminClientAsync(false, ct);
        using var denied = await restricted.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task JsonAndMultipartWritesRejectMissingAntiforgery()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AdminIdentityApiFactory();
        using var client = await factory.CreateAdminClientAsync(true, ct);
        client.DefaultRequestHeaders.Remove("X-Templar-CSRF");
        using var create = await client.PostAsJsonAsync("/api/v1/templates", new { name = "Protected", key = "protected", sections = Array.Empty<object>() }, ct);
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        Assert.Equal("antiforgery-failed", (await create.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("code").GetString());
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent([1, 2, 3]), "file", "test.png");
        using var upload = await client.PostAsync($"/api/v1/media/assets?folderId={Guid.NewGuid()}", form, ct);
        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
        Assert.Equal("antiforgery-failed", (await upload.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task BootstrapAdminCannotLoseItsRoleAndEmailSyncIsAtomic()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AdminIdentityApiFactory();
        using var client = await factory.CreateAdminClientAsync(true, ct);
        var path = $"/api/v1/security/users/{factory.UserId}";
        var profile = await client.GetFromJsonAsync<JsonElement>(path, ct);
        Assert.NotEqual(JsonValueKind.Null, profile.GetProperty("lastLogin").ValueKind);
        var revision = profile.GetProperty("revision").GetGuid();
        using var remove = await client.PutAsJsonAsync(path, new { firstName = "Templar", lastName = "Administrator", email = "admin@example.test", language = "en", roles = Array.Empty<string>(), revision }, ct);
        Assert.Equal(HttpStatusCode.Conflict, remove.StatusCode);
        Assert.Equal("bootstrap-admin-protected", (await remove.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("code").GetString());
        using var update = await client.PutAsJsonAsync(path, new { firstName = "Templar", lastName = "Administrator", email = "changed@example.test", language = "en", roles = new[] { "TemplarAdmin" }, revision }, ct);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();
        var account = await db.Users.AsNoTracking().SingleAsync(ct);
        Assert.Equal("changed@example.test", account.Email);
        Assert.Equal("CHANGED@EXAMPLE.TEST", account.NormalizedEmail);
        Assert.Equal("TemplarAdmin", account.UserName);
    }

    [Fact]
    public async Task NewProfilesStayInvitedAndRoleChangesTakeEffectImmediately()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AdminIdentityApiFactory();
        using var client = await factory.CreateAdminClientAsync(true, ct);
        using var pending = await client.PostAsJsonAsync("/api/v1/security/users", new { firstName = "Pending", lastName = "Author", email = "pending@example.test", language = "en", roles = new[] { "Author" } }, ct);
        Assert.Equal(HttpStatusCode.Created, pending.StatusCode);
        Assert.Equal("invited", (await pending.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("status").GetString());
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();
            Assert.Single(await db.Users.ToListAsync(ct));
            var profile = await db.DirectoryUsers.SingleAsync(value => value.Id == factory.UserId, ct);
            profile.RolesJson = "[]";
            await db.SaveChangesAsync(ct);
        }
        using var denied = await client.GetAsync("/api/v1/security/users", ct);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }
}
