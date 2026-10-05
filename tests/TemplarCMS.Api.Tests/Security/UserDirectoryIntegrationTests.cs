using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TemplarCMS.Api.Security;
using TemplarCMS.Persistence;
using Xunit;

namespace TemplarCMS.Api.Tests.Security;

public sealed class UserDirectoryIntegrationTests
{
    private static DirectoryProfileRequest Profile(string email = "author@example.test", Guid? revision = null) =>
        new(" Test ", " Author ", email, ["Author"], "EN", revision);

    [Theory]
    [InlineData("GET", "/api/v1/security/users")]
    [InlineData("GET", "/api/v1/security/roles")]
    [InlineData("GET", "/api/v1/security/users/11111111-1111-1111-1111-111111111111")]
    [InlineData("POST", "/api/v1/security/users")]
    [InlineData("PUT", "/api/v1/security/users/11111111-1111-1111-1111-111111111111")]
    public async Task DirectoryRequiresAuthentication(string method, string path)
    {
        await using var factory = new DirectoryFactory();
        using var client = factory.CreateClient();
        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DisabledAuthoringSecurityDoesNotMakeDirectoryAnonymous()
    {
        await using var factory = new DirectoryFactory(securityEnabled: false);
        using var client = factory.ClientWithKey();
        using var response = await client.GetAsync("/api/v1/security/users", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DirectoryMustBeExplicitlyEnabled()
    {
        await using var factory = new DirectoryFactory(directoryEnabled: false);
        using var client = factory.ClientWithKey();
        using var response = await client.GetAsync("/api/v1/security/users", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task ProfilesPersistAndRejectStaleEditsWithoutChangingStatus()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new DirectoryFactory();
        using var client = factory.ClientWithKey();
        using var created = await client.PostAsJsonAsync("/api/v1/security/users", Profile(), ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var user = await created.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal("invited", user.GetProperty("status").GetString());
        Assert.Equal("Test", user.GetProperty("firstName").GetString());
        Assert.Equal("en", user.GetProperty("language").GetString());
        Assert.Equal(JsonValueKind.Null, user.GetProperty("lastLogin").ValueKind);
        var path = created.Headers.Location!.ToString();
        var revision = user.GetProperty("revision").GetGuid();
        using var update = await client.PutAsJsonAsync(path, Profile(revision: revision) with { Roles = ["Developer", "FormsEditor"] }, ct);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        using var stale = await client.PutAsJsonAsync(path, Profile(revision: revision), ct);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var reloaded = await client.GetAsync(path, ct);
        var saved = await reloaded.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal("invited", saved.GetProperty("status").GetString());
        Assert.Equal(2, saved.GetProperty("roles").GetArrayLength());
        Assert.True(reloaded.Headers.CacheControl?.NoStore);
        using var filtered = await client.GetAsync("/api/v1/security/users?role=Developer&status=invited&search=TEST", ct);
        var list = await filtered.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal(1, list.GetProperty("total").GetInt32());
        using var empty = await client.GetAsync("/api/v1/security/users?status=active", ct);
        Assert.Equal(0, (await empty.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task EmailUniquenessIsCaseInsensitiveAndWhitespaceNormalized()
    {
        await using var factory = new DirectoryFactory();
        using var client = factory.ClientWithKey();
        var ct = TestContext.Current.CancellationToken;
        using var first = await client.PostAsJsonAsync("/api/v1/security/users", Profile(), ct);
        using var duplicate = await client.PostAsJsonAsync("/api/v1/security/users", Profile(" AUTHOR@example.test "), ct);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Theory]
    [InlineData("status", "active")]
    [InlineData("password", "must-not-be-accepted")]
    public async Task CreationRejectsLifecycleAndCredentialProperties(string property, string value)
    {
        await using var factory = new DirectoryFactory();
        using var client = factory.ClientWithKey();
        var payload = new Dictionary<string, object> { ["firstName"] = "Test", ["lastName"] = "Author",
            ["email"] = "author@example.test", ["roles"] = new[] { "Author" }, [property] = value };
        using var response = await client.PostAsJsonAsync("/api/v1/security/users", payload, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownAndDuplicateRolesAreRejected()
    {
        await using var factory = new DirectoryFactory();
        using var client = factory.ClientWithKey();
        foreach (var roles in new[] { new[] { "Unknown" }, new[] { "0" }, new[] { "Author", "Author" } })
        {
            using var response = await client.PostAsJsonAsync("/api/v1/security/users", Profile() with { Roles = roles }, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact]
    public async Task LegacySqliteDatabaseReceivesDirectoryTableWithoutLosingContent()
    {
        await using var factory = new DirectoryFactory();
        var options = new DbContextOptionsBuilder<TemplarCmsDbContext>().UseSqlite(factory.ConnectionString).Options;
        await using (var db = new TemplarCmsDbContext(options))
        {
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            await db.Database.ExecuteSqlRawAsync("DROP TABLE DirectoryUsers", TestContext.Current.CancellationToken);
        }
        using var client = factory.ClientWithKey();
        using var response = await client.GetAsync("/api/v1/security/users", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();
        Assert.True(await database.ContentItems.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CatalogExposesFiveRolesWithFourAssignableEntries()
    {
        await using var factory = new DirectoryFactory();
        using var client = factory.ClientWithKey();
        var catalog = await client.GetFromJsonAsync<JsonElement>("/api/v1/security/roles", TestContext.Current.CancellationToken);
        var entries = catalog.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(new[] { "Author", "Developer", "FormsEditor", "TemplarAdmin", "MarketingAutomationEditors" },
            entries.Select(entry => entry.GetProperty("id").GetString()));
        Assert.Equal(4, entries.Count(entry => entry.GetProperty("isAssignable").GetBoolean()));
        Assert.All(entries, entry => Assert.False(string.IsNullOrWhiteSpace(entry.GetProperty("description").GetString())));
        Assert.Equal("Forms Editor", entries[2].GetProperty("label").GetString());
        Assert.False(entries[4].GetProperty("isAssignable").GetBoolean());
        Assert.Equal("planned", entries[4].GetProperty("availability").GetString());
        Assert.Equal("/api/v1/security/roles", catalog.GetProperty("_links").GetProperty("self").GetProperty("href").GetString());
    }

    [Theory]
    [InlineData("PlatformAdministrator")]
    [InlineData("SecurityAdministrator")]
    [InlineData("TemplateDesigner")]
    [InlineData("Publisher")]
    [InlineData("ContentAuthor")]
    [InlineData("MediaManager")]
    [InlineData("Reviewer")]
    [InlineData("Unknown")]
    [InlineData("author")]
    [InlineData("0")]
    [InlineData("MarketingAutomationEditors")]
    public async Task InvalidOrUnassignableRolesCannotCreateOrChangeProfiles(string role)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new DirectoryFactory();
        using var client = factory.ClientWithKey();
        using var created = await client.PostAsJsonAsync("/api/v1/security/users", Profile(), ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var original = await created.Content.ReadFromJsonAsync<JsonElement>(ct);
        var path = created.Headers.Location!.ToString();
        var invalid = Profile("other@example.test", original.GetProperty("revision").GetGuid()) with { Roles = [role] };
        using var post = await client.PostAsJsonAsync("/api/v1/security/users", invalid, ct);
        using var put = await client.PutAsJsonAsync(path, invalid, ct);
        foreach (var response in new[] { post, put })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            Assert.True(problem.GetProperty("errors").TryGetProperty("roles", out _));
        }
        var saved = await client.GetFromJsonAsync<JsonElement>(path, ct);
        Assert.Equal(original.GetProperty("revision").GetGuid(), saved.GetProperty("revision").GetGuid());
        Assert.Equal("Author", saved.GetProperty("roles")[0].GetString());
        var list = await client.GetFromJsonAsync<JsonElement>("/api/v1/security/users", ct);
        Assert.Equal(1, list.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task EmptyAndMultipleMembershipsRoundTripAsStringKeys()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new DirectoryFactory();
        using var client = factory.ClientWithKey();
        using var created = await client.PostAsJsonAsync("/api/v1/security/users", Profile() with { Roles = [] }, ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var user = await created.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Empty(user.GetProperty("roles").EnumerateArray());
        using var updated = await client.PutAsJsonAsync(created.Headers.Location, Profile(revision: user.GetProperty("revision").GetGuid())
            with { Roles = ["TemplarAdmin", "Author", "Developer", "FormsEditor"] }, ct);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();
        var row = await db.DirectoryUsers.AsNoTracking().SingleAsync(ct);
        Assert.Equal(new[] { "Author", "Developer", "FormsEditor", "TemplarAdmin" }, JsonSerializer.Deserialize<string[]>(row.RolesJson));
        var reloaded = await client.GetFromJsonAsync<JsonElement>(created.Headers.Location, ct);
        Assert.Equal(4, reloaded.GetProperty("roles").GetArrayLength());
    }

    [Fact]
    public async Task OpenApiDescribesCatalogMetadataAndRoleWriteConstraints()
    {
        await using var factory = new DirectoryFactory();
        using var client = factory.ClientWithKey();
        var doc = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json", TestContext.Current.CancellationToken);
        var schemas = doc.GetProperty("components").GetProperty("schemas");
        var metadata = schemas.GetProperty("DirectoryRoleResponse").GetProperty("properties");
        Assert.True(metadata.TryGetProperty("description", out _));
        Assert.True(metadata.TryGetProperty("isAssignable", out _));
        Assert.True(metadata.TryGetProperty("availability", out _));
        var roles = schemas.GetProperty("DirectoryProfileRequest").GetProperty("properties").GetProperty("roles");
        Assert.True(roles.GetProperty("uniqueItems").GetBoolean());
        Assert.Equal(new[] { "Author", "Developer", "FormsEditor", "TemplarAdmin", "MarketingAutomationEditors" },
            roles.GetProperty("items").GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
        Assert.Contains("preserved or removed", roles.GetProperty("description").GetString());
        var operation = doc.GetProperty("paths").GetProperty("/api/v1/security/users/{id}").GetProperty("put");
        Assert.True(operation.GetProperty("responses").TryGetProperty("409", out _));
        Assert.True(operation.TryGetProperty("security", out _));
    }

    [Theory]
    [InlineData("Reviewer")]
    [InlineData("author")]
    [InlineData("0")]
    public async Task InvalidRoleFiltersAreRejected(string role)
    {
        await using var factory = new DirectoryFactory();
        using var client = factory.ClientWithKey();
        using var response = await client.GetAsync($"/api/v1/security/users?role={role}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PlannedMembershipCanBePreservedOrRemovedButNotReadded()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new DirectoryFactory();
        using var client = factory.ClientWithKey();
        using var created = await client.PostAsJsonAsync("/api/v1/security/users", Profile(), ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var original = await created.Content.ReadFromJsonAsync<JsonElement>(ct);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();
            var row = await db.DirectoryUsers.SingleAsync(ct);
            row.RolesJson = "[\"Author\",\"MarketingAutomationEditors\"]";
            await db.SaveChangesAsync(ct);
        }
        var path = created.Headers.Location!.ToString();
        using var preserved = await client.PutAsJsonAsync(path, Profile(revision: original.GetProperty("revision").GetGuid())
            with { Roles = ["Author", "MarketingAutomationEditors"] }, ct);
        Assert.Equal(HttpStatusCode.OK, preserved.StatusCode);
        var saved = await preserved.Content.ReadFromJsonAsync<JsonElement>(ct);
        var filtered = await client.GetFromJsonAsync<JsonElement>("/api/v1/security/users?role=MarketingAutomationEditors", ct);
        Assert.Equal(1, filtered.GetProperty("total").GetInt32());
        using var removed = await client.PutAsJsonAsync(path, Profile(revision: saved.GetProperty("revision").GetGuid()) with { Roles = [] }, ct);
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        var empty = await removed.Content.ReadFromJsonAsync<JsonElement>(ct);
        using var stale = await client.PutAsJsonAsync(path, Profile(revision: saved.GetProperty("revision").GetGuid())
            with { Roles = ["MarketingAutomationEditors"] }, ct);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var readded = await client.PutAsJsonAsync(path, Profile(revision: empty.GetProperty("revision").GetGuid())
            with { Roles = ["MarketingAutomationEditors"] }, ct);
        Assert.Equal(HttpStatusCode.BadRequest, readded.StatusCode);
        var final = await client.GetFromJsonAsync<JsonElement>(path, ct);
        Assert.Empty(final.GetProperty("roles").EnumerateArray());
    }

    private sealed class DirectoryFactory(bool securityEnabled = true, bool directoryEnabled = true) : WebApplicationFactory<Program>
    {
        private readonly string root = Directory.CreateTempSubdirectory("TemplarCMS.DirectoryTests-").FullName;
        public string ConnectionString => $"Data Source={Path.Combine(root, "directory.db")};Pooling=False";
        public HttpClient ClientWithKey()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Templar-Api-Key", "directory-test-key");
            return client;
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.CreateDirectory(root);
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "Sqlite", ["ConnectionStrings:TemplarCms"] = ConnectionString,
                ["Templates:TemplatesPath"] = Path.Combine(root, "Templates"),
                ["AuthoringSecurity:Enabled"] = securityEnabled.ToString(), ["AuthoringSecurity:ApiKey"] = "directory-test-key",
                ["UserDirectory:Enabled"] = directoryEnabled.ToString()
            }));
        }
        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
