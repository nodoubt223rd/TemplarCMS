using TemplarCMS.ContentModeling.Organization;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.Application.Templates;

/// <summary>Organization operations run inside the shared template mutation coordinator.</summary>
public sealed class TemplateOrganizationService(JsonTemplateOrganizationRepository organization,
    ITemplateRepository templates, IBuiltInTemplateProvider builtIns)
{
    public Task<TemplateOrganizationSnapshot> ReadAsync(CancellationToken ct) => organization.ReadAsync(ct);

    public async Task<TemplateOrganizationSnapshot> ChangeFolderAsync(string action, Guid? id, string? name,
        string? key, Guid? parentId, Guid expectedRevision, CancellationToken ct)
    {
        var state = await ReadAsync(ct);
        CheckRevision(state, expectedRevision);
        var folders = state.Folders.ToList();
        var parent = parentId is { } p ? new TemplateFolderId(p) : (TemplateFolderId?)null;
        var folder = folders.FirstOrDefault(f => f.Id.Value == id);
        if (action != "create" && folder is null) throw new KeyNotFoundException("Template folder was not found.");
        switch (action)
        {
            case "create":
                folders.Add(new(new(Guid.NewGuid()), name!, key!, parent));
                break;
            case "rename":
                folders[folders.IndexOf(folder!)] = new(folder!.Id, name!, folder.Key, folder.ParentId);
                break;
            case "move":
                folders[folders.IndexOf(folder!)] = new(folder!.Id, folder.Name, folder.Key, parent);
                break;
            case "delete":
                if (folders.Any(f => f.ParentId == folder!.Id) || state.Placements.Any(p => p.ParentId == folder!.Id))
                    throw new TemplateOrganizationConflictException("Move or delete this folder's children first.");
                folders.Remove(folder!);
                break;
            default: throw new ArgumentException("Unknown folder action.");
        }
        if (parent is { } destination && !state.Folders.Any(f => f.Id == destination))
            throw new KeyNotFoundException("Destination folder was not found.");
        return await SaveAsync(state with { Folders = folders }, ct);
    }

    public async Task<TemplateOrganizationSnapshot> ChangeTemplateAsync(Guid id, string action, string? name,
        Guid? parentId, Guid revision, CancellationToken ct)
    {
        var state = await ReadAsync(ct);
        CheckRevision(state, revision);
        var template = (await templates.GetTemplatesAsync(ct)).FirstOrDefault(t => t.Id.Value == id)
            ?? throw new KeyNotFoundException("Template was not found.");
        if (builtIns.GetTemplates().Any(t => t.Id == template.Id))
            throw new TemplateOrganizationConflictException("System templates cannot be renamed or moved.");
        if (action == "rename")
        {
            var updated = new TemplateDefinition(template.Id, name!, template.Key,
                sections: template.Sections, icon: template.Icon, baseTemplates: template.BaseTemplates);
            await templates.UpdateTemplateAsync(template.Key, updated, ct);
        }
        else if (action == "move")
        {
            var placements = state.Placements.Where(p => p.TemplateId != template.Id).ToList();
            if (parentId is { } parent)
            {
                var destination = new TemplateFolderId(parent);
                if (!state.Folders.Any(f => f.Id == destination)) throw new KeyNotFoundException("Destination folder was not found.");
                placements.Add(new(template.Id, destination));
            }
            state = state with { Placements = placements };
        }
        else throw new ArgumentException("Unknown template action.");
        return await SaveAsync(state, ct);
    }

    public async Task<TemplateOrganizationSnapshot> SaveAsync(TemplateOrganizationSnapshot state, CancellationToken ct)
    {
        var validation = TemplateOrganizationValidator.Validate(state);
        if (!validation.IsValid) throw new ArgumentException(string.Join(" ", validation.Errors.Select(e => e.Message)));
        await organization.SaveLockedAsync(state, state.Revision, ct);
        return await ReadAsync(ct);
    }

    public static void CheckRevision(TemplateOrganizationSnapshot state, Guid revision)
    {
        if (state.Revision != revision) throw new TemplateOrganizationConflictException("Template organization changed. Refresh and try again.");
    }
}
