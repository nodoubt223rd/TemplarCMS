using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using TemplarCMS.Application.Media;
using TemplarCMS.Domain.Media;
using TemplarCMS.Domain.Content;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TemplarCMS.Api.Security;
using TemplarCMS.Api.Tests.Support;
using Xunit;

namespace TemplarCMS.Api.Tests.Security;

public sealed class DeliveryAuthorizationTests
{
    [Fact]
    public async Task MixedCredentialAntiforgeryRefreshBindsToTheAccountForPasswordChange()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new DeliveryApiFactory();
        using var client = await factory.CreateAdminClientAsync(false, ct);
        SetKey(client, factory.SiteKey.Key);
        await AdminIdentityApiFactory.RefreshCsrfAsync(client, ct);
        using var change = await client.PostAsJsonAsync("/api/v1/auth/change-password",
            new { currentPassword = factory.TemporaryPassword, newPassword = "a fresh mixed credential passphrase" }, ct);
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
    }

    [Fact]
    public async Task SharedReadsDistinguishMissingWrongValidAndRestrictedCredentials()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new DeliveryApiFactory();
        using var client = factory.CreateAnonymousClient();
        using var missing = await client.GetAsync("/api/v1/content/root/children", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        client.DefaultRequestHeaders.Add(DeliveryApiKeyDefaults.HeaderName, "wrong");
        using var wrong = await client.GetAsync("/api/v1/content/root/children", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        SetKey(client, factory.SiteKey.Key);
        using var allowed = await client.GetAsync("/api/v1/content/root/children", ct);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.True(allowed.Headers.CacheControl?.NoStore);
        Assert.True(allowed.Headers.CacheControl?.Private);
        var roots = await allowed.Content.ReadFromJsonAsync<JsonElement>(ct);
        var item = roots.GetProperty("embedded").GetProperty("items")[0];
        var id = item.GetProperty("id").GetString();
        var path = item.GetProperty("path").GetString()!.TrimStart('/');
        foreach (var route in new[] { $"/api/v1/content/{id}", $"/api/v1/content/by-path/{path}", $"/api/v1/content/{id}/children" })
        {
            using var response = await client.GetAsync(route, ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
        }
        foreach (var forbiddenLink in new[] { "template", "dependencies", "branch", "set-values", "rename", "move", "delete" })
            Assert.False(item.GetProperty("_links").TryGetProperty(forbiddenLink, out _));
        using var restricted = await factory.CreateAdminClientAsync(false, ct);
        using var restrictedRead = await restricted.GetAsync("/api/v1/content/root/children", ct);
        Assert.Equal(HttpStatusCode.Forbidden, restrictedRead.StatusCode);
        Assert.Equal("password-change-required", (await restrictedRead.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("code").GetString());
        SetKey(restricted, factory.SiteKey.Key);
        using var mixedSession = await restricted.GetAsync("/api/v1/auth/session", ct);
        Assert.Equal(HttpStatusCode.OK, mixedSession.StatusCode);
        Assert.Equal(factory.UserId.ToString(), (await mixedSession.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("userId").GetString());
        using var mixed = await restricted.GetAsync("/api/v1/content/root/children", ct);
        Assert.Equal(HttpStatusCode.OK, mixed.StatusCode);
        using var write = await restricted.PostAsJsonAsync("/api/v1/templates", new { name = "Denied" }, ct);
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        using var admin = await factory.CreateAdminClientAsync(true, ct);
        SetKey(admin, factory.SiteKey.Key);
        using var validMixedWrite = await admin.PostAsJsonAsync("/api/v1/templates", new { name = "Mixed Test", key = "mixed-test", sections = Array.Empty<object>() }, ct);
        Assert.Equal(HttpStatusCode.Created, validMixedWrite.StatusCode);
        SetKey(admin, "invalid");
        using var adminRead = await admin.GetAsync("/api/v1/content/root/children", ct);
        Assert.Equal(HttpStatusCode.OK, adminRead.StatusCode);
        var adminItems = await adminRead.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.True(adminItems.GetProperty("embedded").GetProperty("items")[0].GetProperty("_links").TryGetProperty("template", out _));
        using var adminWrite = await admin.PostAsJsonAsync("/api/v1/templates", new { name = "Delivery Test", key = "delivery-test", sections = Array.Empty<object>() }, ct);
        Assert.Equal(HttpStatusCode.Created, adminWrite.StatusCode);
    }

    [Fact]
    public async Task MediaRangesAndAlternateRawRoutesCannotBypassAuthorization()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new DeliveryApiFactory();
        var asset = new MediaAsset(Guid.NewGuid(), SystemSeedContentIds.Images, "test.png", "test.png", "image/png", 4, null, null, DateTimeOffset.UtcNow);
        await using var media = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddSingleton<IMediaAssetService>(new MediaService(asset))));
        using var client = media.CreateClient(new() { BaseAddress = new("https://localhost"), AllowAutoRedirect = false });
        var route = $"/api/v1/media/assets/{asset.Id}/content";
        client.DefaultRequestHeaders.Range = new(0, 1);
        using var denied = await client.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        SetKey(client, factory.SiteKey.Key);
        using var range = await client.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.PartialContent, range.StatusCode);
        Assert.Equal(new byte[] { 1, 2 }, await range.Content.ReadAsByteArrayAsync(ct));
        Assert.True(range.Headers.CacheControl?.NoStore);
        Assert.True(range.Headers.CacheControl?.Private);
        var endpoints = media.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();
        var reads = endpoints.Where(endpoint => endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(data => data.Policy == ApiAuthorizationPolicies.ReadContent)).ToArray();
        Assert.Equal(5, reads.Length);
        Assert.DoesNotContain(endpoints.Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null), endpoint =>
            endpoint.RoutePattern.RawText?.StartsWith("/api/v1/content", StringComparison.Ordinal) == true || endpoint.RoutePattern.RawText?.StartsWith("/api/v1/media", StringComparison.Ordinal) == true);
        foreach (var path in new[] { $"/api/v1/content/{Guid.NewGuid()}/branch", $"/api/v1/content/{Guid.NewGuid()}/dependencies" })
        {
            using var privateRead = await client.GetAsync(path, ct);
            Assert.Equal(HttpStatusCode.Forbidden, privateRead.StatusCode);
        }
    }

    [Fact]
    public async Task AuthoringRequirementsCannotCombineClaimsAcrossIdentities()
    {
        await using var factory = new DeliveryApiFactory();
        var authorization = factory.Services.GetRequiredService<IAuthorizationService>();
        var cookie = new ClaimsIdentity([new(ClaimTypes.Role, "TemplarAdmin"), new(AccountSessionValidator.ChangeRequiredClaim, "true")], IdentityConstants.ApplicationScheme);
        var key = new ClaimsIdentity([new(DeliveryApiKeyDefaults.ClaimType, "read"), new(AccountSessionValidator.ChangeRequiredClaim, "false")], DeliveryApiKeyDefaults.SchemeName);
        var principal = new ClaimsPrincipal(new[] { cookie, key });
        Assert.False((await authorization.AuthorizeAsync(principal, null, ApiAuthorizationPolicies.AuthorContent)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(principal, null, ApiAuthorizationPolicies.ReadContent)).Succeeded);
    }

    private sealed class MediaService(MediaAsset asset) : IMediaAssetService
    {
        public Task<IReadOnlyCollection<MediaAsset>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<MediaAsset>>([asset]);
        public Task<MediaAsset?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(id == asset.Id ? asset : null);
        public Task<Stream?> OpenReadAsync(MediaAsset value, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(new MemoryStream([1, 2, 3, 4]));
        public Task<MediaAsset> CreateAsync(ContentItemId folderId, string fileName, string contentType, Stream content, long length, string? altText, string? title, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [Theory]
    [InlineData("/api/v1/templates")]
    [InlineData("/api/v1/template-organization")]
    [InlineData("/api/v1/field-types")]
    [InlineData("/api/v1/content/root/branch")]
    [InlineData("/api/v1/content/workspaces/content/branch")]
    [InlineData("/api/v1/media/assets")]
    [InlineData("/api/v1/security/users")]
    [InlineData("/api/v1/security/roles")]
    [InlineData("/api/v1/auth/session")]
    public async Task DeliveryCredentialsCannotReadAdministratorRoutes(string route)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new DeliveryApiFactory();
        using var client = factory.CreateAnonymousClient();
        SetKey(client, factory.SiteKey.Key);
        using var response = await client.GetAsync(route, ct);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal(403, problem.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task KeysCannotWriteAndMalformedOrQueryCredentialsCannotAuthenticate()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new DeliveryApiFactory();
        using var client = factory.CreateAnonymousClient();
        SetKey(client, factory.SiteKey.Key);
        foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Delete })
        {
            var route = method == HttpMethod.Post ? "/api/v1/content" : $"/api/v1/content/{Guid.NewGuid()}";
            using var response = await client.SendAsync(new(method, route) { Content = JsonContent.Create(new { }) }, ct);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        client.DefaultRequestHeaders.Remove(DeliveryApiKeyDefaults.HeaderName);
        using var query = await client.GetAsync($"/api/v1/content/root/children?X-Templar-Api-Key={factory.SiteKey.Key}", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, query.StatusCode);
        client.DefaultRequestHeaders.TryAddWithoutValidation(DeliveryApiKeyDefaults.HeaderName, new[] { factory.SiteKey.Key, factory.SiteKey.Key });
        using var duplicate = await client.GetAsync("/api/v1/content/root/children", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, duplicate.StatusCode);
        SetKey(client, new string('A', 10_000));
        using var oversized = await client.GetAsync("/api/v1/content/root/children", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, oversized.StatusCode);
        SetKey(client, factory.SiteKey.Key);
        using var insecure = await client.GetAsync("http://localhost/api/v1/content/root/children", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, insecure.StatusCode);
    }

    [Fact]
    public async Task NoConfiguredKeysAndRetiredOperatorKeyGrantNoAccess()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new DeliveryApiFactory { Overrides = new() { ["AuthoringSecurity:ApiKey"] = "retired-operator-key" } };
        using var client = factory.CreateAnonymousClient();
        foreach (var key in new[] { factory.SiteKey.Key, "retired-operator-key" })
        {
            SetKey(client, key);
            using var read = await client.GetAsync("/api/v1/content/root/children", ct);
            Assert.Equal(HttpStatusCode.Unauthorized, read.StatusCode);
        }
    }

    internal static void SetKey(HttpClient client, string key)
    {
        client.DefaultRequestHeaders.Remove(DeliveryApiKeyDefaults.HeaderName);
        client.DefaultRequestHeaders.Add(DeliveryApiKeyDefaults.HeaderName, key);
    }

    [Fact]
    public async Task RequestAndAuthenticationLogsDoNotContainKeysOrDigests()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new DeliveryApiFactory();
        var messages = new ConcurrentQueue<string>();
        await using var logged = factory.WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.AddProvider(new CaptureProvider(messages))));
        using var client = logged.CreateClient(new() { BaseAddress = new("https://localhost") });
        SetKey(client, factory.SiteKey.Key);
        using var success = await client.GetAsync("/api/v1/content/root/children", ct);
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        client.DefaultRequestHeaders.Remove(DeliveryApiKeyDefaults.HeaderName);
        using var denied = await client.GetAsync($"/api/v1/content/root/children?key={factory.SiteKey.Key}", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        Assert.NotEmpty(messages);
        var log = string.Join('\n', messages);
        Assert.DoesNotContain(factory.SiteKey.Key, log);
        Assert.DoesNotContain(factory.SiteKey.Sha256Digest, log);
    }

    private sealed class CaptureProvider(ConcurrentQueue<string> messages) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(messages);
        public void Dispose() { }
    }
    private sealed class CaptureLogger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => messages.Enqueue(formatter(state, exception) + exception);
    }
}
