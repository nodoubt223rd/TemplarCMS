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
    public async Task StartupGroupsBuiltInsAndLeavesMediaEmpty()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        var state = (await client.GetFromJsonAsync<TemplateOrganizationSnapshot>("/api/v1/template-organization", ct))!;
        Assert.Equal(new[] { "Common", "Media", "System" }, state.Folders.Select(f => f.Name).Order().ToArray());
        Assert.All(state.Folders, folder => Assert.Null(folder.ParentId));
        var builtIns = new TemplarCMS.ContentModeling.Definitions.BuiltInTemplateProvider().GetTemplates();
        foreach (var template in builtIns)
        {
            var folder = state.Folders.Single(f => f.Name == (template.Key.ToString() is "folder" or "template-folder" ? "Common" : "System"));
            Assert.Contains(state.Placements, p => p.TemplateId == template.Id && p.ParentId == folder.Id);
        }
        var media = state.Folders.Single(f => f.Name == "Media");
        Assert.DoesNotContain(state.Placements, p => p.ParentId == media.Id);
        Assert.DoesNotContain(state.Placements, p => p.TemplateId.Value == new Guid("562BA716-A878-45E5-9BA7-397F46BA7B1D"));
    }

    [Fact]
    public async Task TemplateDetailsKeepAuthoredIconSeparateFromInheritedDisplay()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Templar-Api-Key", "organization-tests");
        var ct = TestContext.Current.CancellationToken;
        using var baseResponse = await client.PostAsJsonAsync("/api/v1/templates", new { name = "Base", key = "icon-base", icon = "star", sections = Array.Empty<object>() }, ct);
        baseResponse.EnsureSuccessStatusCode();
        var parent = (await baseResponse.Content.ReadFromJsonAsync<TemplateResponse>(ct))!;
        using var childResponse = await client.PostAsJsonAsync("/api/v1/templates", new { name = "Child", key = "icon-child", baseTemplateKeys = new[] { "icon-base" }, sections = Array.Empty<object>() }, ct);
        childResponse.EnsureSuccessStatusCode();
        var child = (await childResponse.Content.ReadFromJsonAsync<TemplateResponse>(ct))!;
        Assert.Null(child.AuthoredIcon);
        Assert.Equal("star", child.Icon);
        using var update = await client.PutAsJsonAsync($"/api/v1/templates/{parent.Id}", new { name = "Base", key = "icon-base", icon = "article", sections = Array.Empty<object>() }, ct);
        update.EnsureSuccessStatusCode();
        var inherited = (await client.GetFromJsonAsync<TemplateResponse>($"/api/v1/templates/{child.Id}", ct))!;
        Assert.Null(inherited.AuthoredIcon);
        Assert.Equal("article", inherited.Icon);
    }

    [Fact]
    public async Task UpgradeReusesExistingFolderAndPreservesCustomPlacement()
    {
        var path = Path.Combine(Path.GetTempPath(), "templar-org-upgrade-" + Guid.NewGuid().ToString("N"));
        var ct = TestContext.Current.CancellationToken;
        var common = new TemplateFolderDefinition(new(Guid.NewGuid()), "Common", "COMMON", null);
        var custom = new TemplateFolderDefinition(new(Guid.NewGuid()), "Custom", "custom", common.Id);
        var pageId = new TemplateId(new Guid("562BA716-A878-45E5-9BA7-397F46BA7B1D"));
        try
        {
            var repository = new JsonTemplateOrganizationRepository(Path.Combine(path, "Templates"));
            await repository.SaveAsync(new(Guid.Empty, [common, custom], [new(pageId, custom.Id)]), Guid.Empty, ct);
            var before = await repository.ReadAsync(ct);
            await using var factory = new Factory(path, false);
            using var client = factory.CreateClient();
            var after = (await client.GetFromJsonAsync<TemplateOrganizationSnapshot>("/api/v1/template-organization", ct))!;
            Assert.NotEqual(before.Revision, after.Revision);
            Assert.Equal(4, after.Folders.Count);
            Assert.Equal(common.Id, Assert.Single(after.Folders, f => f.Key.Equals("common", StringComparison.OrdinalIgnoreCase)).Id);
            Assert.Contains(after.Folders, f => f.Id == custom.Id && f.ParentId == common.Id);
            Assert.Contains(after.Placements, p => p.TemplateId == pageId && p.ParentId == custom.Id);
            Assert.Contains(after.Placements, p => p.TemplateId == SystemTemplateIds.TemplateFolder && p.ParentId == common.Id);
            Assert.Equal(2, after.Folders.Count - before.Folders.Count);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (Directory.Exists(path)) Directory.Delete(path, true); }
    }

    [Fact]
    public async Task OrganizationSurvivesApplicationRestart()
    {
        var path = Path.Combine(Path.GetTempPath(), "templar-restart-" + Guid.NewGuid().ToString("N"));
        var ct = TestContext.Current.CancellationToken;
        Guid revision;
        try
        {
            await using (var first = new Factory(path, false))
            {
                using var client = first.CreateClient();
                client.DefaultRequestHeaders.Add("X-Templar-Api-Key", "organization-tests");
                var initial = (await client.GetFromJsonAsync<TemplateOrganizationSnapshot>("/api/v1/template-organization", ct))!;
                using var response = await client.PostAsJsonAsync("/api/v1/template-folders", new { name = "Retained", key = "retained", expectedRevision = initial.Revision }, ct);
                response.EnsureSuccessStatusCode();
                revision = (await response.Content.ReadFromJsonAsync<TemplateOrganizationSnapshot>(ct))!.Revision;
            }
            await using var second = new Factory(path, false);
            using var reader = second.CreateClient();
            var restored = (await reader.GetFromJsonAsync<TemplateOrganizationSnapshot>("/api/v1/template-organization", ct))!;
            Assert.Equal(revision, restored.Revision);
            Assert.Equal(4, restored.Folders.Count);
            Assert.Contains(restored.Folders, f => f.Name == "Retained");
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); if (Directory.Exists(path)) Directory.Delete(path, true); }
    }
    [Fact]
    public async Task FolderAndTemplateMutationsPreserveContainmentAndRejectStaleState()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        using var unauthorized = await client.PostAsJsonAsync("/api/v1/template-folders", new { name = "Pages", key = "pages", expectedRevision = Guid.Empty }, ct);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        client.DefaultRequestHeaders.Add("X-Templar-Api-Key", "organization-tests");
        var initial = (await client.GetFromJsonAsync<TemplateOrganizationSnapshot>("/api/v1/template-organization", ct))!;
        using var created = await client.PostAsJsonAsync("/api/v1/template-folders", new { name = "Pages", key = "pages", expectedRevision = initial.Revision }, ct);
        created.EnsureSuccessStatusCode();
        var state = (await created.Content.ReadFromJsonAsync<TemplateOrganizationSnapshot>(ct))!;
        var folder = Assert.Single(state.Folders, f => f.Key == "pages");
        using var stale = await client.PostAsJsonAsync($"/api/v1/template-folders/{folder.Id}/rename", new { name = "Changed", expectedRevision = Guid.Empty }, ct);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var cycle = await client.PostAsJsonAsync($"/api/v1/template-folders/{folder.Id}/move", new { parentId = folder.Id.Value, expectedRevision = state.Revision }, ct);
        Assert.Equal(HttpStatusCode.BadRequest, cycle.StatusCode);
        using var template = await client.PostAsJsonAsync("/api/v1/templates", new CreateTemplateRequest
        { Name = "Landing", Key = "landing", Sections = [], ParentFolderId = folder.Id.Value, ExpectedOrganizationRevision = state.Revision }, ct);
        template.EnsureSuccessStatusCode();
        var definition = (await template.Content.ReadFromJsonAsync<TemplateResponse>(ct))!;
        state = (await client.GetFromJsonAsync<TemplateOrganizationSnapshot>("/api/v1/template-organization", ct))!;
        Assert.Equal(definition.Id, Assert.Single(state.Placements, p => p.ParentId == folder.Id).TemplateId.ToString());
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

    private sealed class Factory(string? runtimeRoot = null, bool deleteOnDispose = true) : WebApplicationFactory<Program>
    {
        private readonly string root = runtimeRoot ?? Path.Combine(Path.GetTempPath(), "templar-org-api-" + Guid.NewGuid().ToString("N"));
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
            try { if (deleteOnDispose && Directory.Exists(root)) Directory.Delete(root, true); }
            catch (IOException) { }
        }
    }
}
