using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TemplarCMS.Api.Tests.Support;
using TemplarCMS.Persistence;
using Xunit;

namespace TemplarCMS.Api.Tests.Security;

public sealed class AuthoringSecurityIntegrationTests
{
    [Fact]
    public async Task SampleHomePageStillRendersPublicHtml()
    {
        await using var factory = new AdminIdentityApiFactory();
        using var client = factory.CreateAnonymousClient();
        using var response = await client.GetAsync("/", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<title>Home | TemplarCMS</title>", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("POST", "/api/v1/content")]
    [InlineData("PUT", "/api/v1/content/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/v1/content/00000000-0000-0000-0000-000000000001/rename")]
    [InlineData("POST", "/api/v1/content/00000000-0000-0000-0000-000000000001/move")]
    [InlineData("POST", "/api/v1/content/00000000-0000-0000-0000-000000000001/values")]
    [InlineData("POST", "/api/v1/content/00000000-0000-0000-0000-000000000001/reorder")]
    [InlineData("DELETE", "/api/v1/content/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/v1/templates")]
    [InlineData("PUT", "/api/v1/templates/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/v1/templates/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/v1/templates/00000000-0000-0000-0000-000000000001/rename")]
    [InlineData("POST", "/api/v1/templates/00000000-0000-0000-0000-000000000001/move")]
    [InlineData("POST", "/api/v1/template-folders")]
    [InlineData("POST", "/api/v1/template-folders/00000000-0000-0000-0000-000000000001/rename")]
    [InlineData("POST", "/api/v1/template-folders/00000000-0000-0000-0000-000000000001/move")]
    [InlineData("DELETE", "/api/v1/template-folders/00000000-0000-0000-0000-000000000001?expectedRevision=00000000-0000-0000-0000-000000000000")]
    public async Task AuthoringRequiresAnAccountSessionBeforeBodyValidation(string method, string route)
    {
        await using var factory = new AdminIdentityApiFactory();
        using var client = factory.CreateAnonymousClient();
        using var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), route), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("invalid-api-key")]
    [InlineData("templar-test-api-key")]
    public async Task LegacyOperatorKeysDoNotGrantAuthoringAccess(string key)
    {
        await using var factory = new AdminIdentityApiFactory();
        using var client = factory.CreateAnonymousClient();
        client.DefaultRequestHeaders.Add("X-Templar-Api-Key", key);
        using var response = await client.PostAsJsonAsync("/api/v1/templates", new { name = "Rejected", key = "rejected", sections = Array.Empty<object>() }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("authoring-authentication-required", problem.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("/api/v1/templates")]
    [InlineData("/api/v1/template-folders")]
    [InlineData("/api/v1/template-folders/00000000-0000-0000-0000-000000000001/move")]
    [InlineData("/api/v1/templates/00000000-0000-0000-0000-000000000001/rename")]
    [InlineData("/api/v1/content/00000000-0000-0000-0000-000000000001/reorder")]
    public async Task RoleRemovalIsEnforcedOnTheNextRequest(string route)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AdminIdentityApiFactory();
        using var client = await factory.CreateAdminClientAsync(true, ct);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();
            var profile = await db.DirectoryUsers.SingleAsync(ct);
            profile.RolesJson = "[]";
            await db.SaveChangesAsync(ct);
        }
        using var response = await client.PostAsJsonAsync(route, new { name = "Rejected" }, ct);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnrestrictedAdminCanCreateATemplateWithAntiforgery()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AdminIdentityApiFactory();
        using var client = await factory.CreateAdminClientAsync(true, ct);
        using var response = await client.PostAsJsonAsync("/api/v1/templates", new { name = "Authorized", key = "authorized", sections = Array.Empty<object>() }, ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("authorized", (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("key").GetString());
    }
}
