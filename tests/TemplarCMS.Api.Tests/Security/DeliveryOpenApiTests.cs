using System.Net.Http.Json;
using System.Text.Json;
using TemplarCMS.Api.Tests.Support;
using Xunit;

namespace TemplarCMS.Api.Tests.Security;

public sealed class DeliveryOpenApiTests
{
    [Fact]
    public async Task SharedReadsAdvertiseAlternativeCredentialsWhileAuthoringRequiresOnlyAnAccount()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new DeliveryApiFactory();
        using var client = await factory.CreateAdminClientAsync(true, ct);
        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json", ct);
        var schemes = document.GetProperty("components").GetProperty("securitySchemes");
        Assert.Equal("header", schemes.GetProperty("DeliveryApiKey").GetProperty("in").GetString());
        Assert.Equal("X-Templar-Api-Key", schemes.GetProperty("DeliveryApiKey").GetProperty("name").GetString());
        var paths = document.GetProperty("paths");
        foreach (var path in new[] { "/api/v1/content/{id}", "/api/v1/content/by-path/{path}", "/api/v1/content/{id}/children", "/api/v1/content/root/children", "/api/v1/media/assets/{id}/content" })
        {
            var operation = paths.GetProperty(path).GetProperty("get");
            var requirements = operation.GetProperty("security").EnumerateArray().ToArray();
            Assert.Equal(2, requirements.Length);
            Assert.All(requirements, entry => Assert.Single(entry.EnumerateObject()));
            Assert.Contains(requirements, entry => entry.TryGetProperty("AdminSession", out _));
            Assert.Contains(requirements, entry => entry.TryGetProperty("DeliveryApiKey", out _));
            Assert.Contains("password-change-required", operation.GetProperty("responses").GetProperty("403").GetProperty("description").GetString());
        }
        foreach (var (path, method) in new[] { ("/api/v1/templates", "post"), ("/api/v1/security/users", "get"), ("/api/v1/auth/session", "get"), ("/api/v1/auth/change-password", "post"), ("/api/v1/auth/logout", "post") })
        {
            var requirement = Assert.Single(paths.GetProperty(path).GetProperty(method).GetProperty("security").EnumerateArray());
            Assert.True(requirement.TryGetProperty("AdminSession", out _));
            Assert.False(requirement.TryGetProperty("DeliveryApiKey", out _));
        }
        foreach (var (path, method) in new[] { ("/api/v1/auth/login", "post"), ("/api/v1/auth/antiforgery", "get") })
        {
            var operation = paths.GetProperty(path).GetProperty(method);
            Assert.False(operation.TryGetProperty("security", out var security) && security.GetArrayLength() > 0);
        }
        Assert.DoesNotContain(factory.SiteKey.Key, document.GetRawText());
        Assert.DoesNotContain(factory.SiteKey.Sha256Digest, document.GetRawText());
    }
}
