using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Organization;

// A list of placements keeps typed IDs in JSON values, never as dictionary property names.
public sealed record TemplatePlacement(TemplateId TemplateId, TemplateFolderId ParentId);
public sealed record TemplateOrganizationSnapshot(
    Guid Revision,
    IReadOnlyList<TemplateFolderDefinition> Folders,
    IReadOnlyList<TemplatePlacement> Placements)
{
    public static TemplateOrganizationSnapshot Empty => new(Guid.Empty, [], []);
}

public sealed class TemplateOrganizationConflictException(string message) : InvalidOperationException(message);
