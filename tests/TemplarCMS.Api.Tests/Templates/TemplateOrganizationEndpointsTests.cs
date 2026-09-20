using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TemplarCMS.Api.Templates;
using TemplarCMS.ContentModeling.Organization;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.Api.Tests.Templates;

public sealed class TemplateOrganizationEndpointsTests
{
    [Fact]
    public async Task FolderAndTemplateMutationsPreserveContainmentAndRejectStaleState()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        using var unauthorized = await client.PostAsJsonAsync("/api/v1/template-folders", new { name = "Pages", key = "pages", expectedRevision = Guid.Empty }, ct);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        client.DefaultRequestHeaders.Add("X-Templar-Api-Key", "organization-tests");
        using var created = await client.PostAsJsonAsync("/api/v1/template-folders", new { name = "Pages", key = "pages", expectedRevision = Guid.Empty }, ct);
        created.EnsureSuccessStatusCode();
        var state = (await created.Content.ReadFromJsonAsync<TemplateOrganizationSnapshot>(ct))!;
        var folder = Assert.Single(state.Folders);
        using var stale = await client.PostAsJsonAsync($"/api/v1/template-folders/{folder.Id}/rename", new { name = "Changed", expectedRevision = Guid.Empty }, ct);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var cycle = await client.PostAsJsonAsync($"/api/v1/template-folders/{folder.Id}/move", new { parentId = folder.Id.Value, expectedRevision = state.Revision }, ct);
        Assert.Equal(HttpStatusCode.BadRequest, cycle.StatusCode);
        using var template = await client.PostAsJsonAsync("/api/v1/templates", new CreateTemplateRequest
        { Name = "Landing", Key = "landing", Sections = [], ParentFolderId = folder.Id.Value, ExpectedOrganizationRevision = state.Revision }, ct);
        template.EnsureSuccessStatusCode();
        var definition = (await template.Content.ReadFromJsonAsync<TemplateResponse>(ct))!;
        state = (await client.GetFromJsonAsync<TemplateOrganizationSnapshot>("/api/v1/template-organization", ct))!;
        Assert.Equal(definition.Id, Assert.Single(state.Placements).TemplateId.ToString());
        using var nonempty = await client.DeleteAsync($"/api/v1/template-folders/{folder.Id}?expectedRevision={state.Revision}", ct);
        Assert.Equal(HttpStatusCode.Conflict, nonempty.StatusCode);
        using var protectedTemplate = await client.PostAsJsonAsync($"/api/v1/templates/{SystemTemplateIds.Template}/move", new { parentId = folder.Id.Value, expectedRevision = state.Revision }, ct);
        Assert.Equal(HttpStatusCode.Conflict, protectedTemplate.StatusCode);
        using var renamed = await client.PostAsJsonAsync($"/api/v1/templates/{definition.Id}/rename", new { name = "Renamed", expectedRevision = state.Revision }, ct);
        renamed.EnsureSuccessStatusCode();
        var updated = await client.GetFromJsonAsync<TemplateResponse>($"/api/v1/templates/{definition.Id}", ct);
        Assert.Equal("Renamed", updated!.Name);
        Assert.Equal(definition.Key, updated.Key);
    }

    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "templar-org-api-" + Guid.NewGuid().ToString("N"));
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "Sqlite",
                ["ConnectionStrings:TemplarCms"] = $"Data Source={Path.Combine(root, "cms.db")}",
                ["Templates:TemplatesPath"] = Path.Combine(root, "Templates"),
                ["AuthoringSecurity:Enabled"] = "true",
                ["AuthoringSecurity:ApiKey"] = "organization-tests",
                ["OpenApi:Enabled"] = "false"
            }));
        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            try { if (Directory.Exists(root)) Directory.Delete(root, true); }
            catch (IOException) { }
        }
    }
}

