using System.Text.Json.Serialization;
using TemplarCMS.ContentModeling.Organization;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;

namespace TemplarCMS.Api.Templates;

public sealed record TemplateOrganizationResponse(Guid Revision,
    IReadOnlyList<TemplateFolderDefinition> Folders, IReadOnlyList<TemplatePlacement> Placements,
    IReadOnlyList<TemplateOrganizationEntry> Templates,
    [property: JsonPropertyName("_links")] IReadOnlyDictionary<string, object> Links)
{
    public static async Task<TemplateOrganizationResponse> CreateAsync(TemplateOrganizationSnapshot snapshot,
        ITemplateRepository repository, IBuiltInTemplateProvider builtIns, CancellationToken ct)
    {
        var systemIds = builtIns.GetTemplates().Select(t => t.Id).ToHashSet();
        var templates = (await repository.GetTemplatesAsync(ct)).Select(t => new TemplateOrganizationEntry(
            t.Id.Value, systemIds.Contains(t.Id), snapshot.Placements.FirstOrDefault(p => p.TemplateId == t.Id)?.ParentId.Value)).ToArray();
        return new(snapshot.Revision, snapshot.Folders, snapshot.Placements, templates, new Dictionary<string, object>
        {
            ["self"] = new { href = "/api/v1/template-organization" },
            ["createFolder"] = new { href = "/api/v1/template-folders", method = "POST" },
            ["createTemplate"] = new { href = "/api/v1/templates", method = "POST" }
        });
    }
}
public sealed record TemplateOrganizationEntry(Guid Id, bool IsProtected, Guid? ParentId);
