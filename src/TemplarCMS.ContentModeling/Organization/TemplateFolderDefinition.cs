using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Organization;

public sealed class TemplateFolderDefinition : Item
{
    [System.Text.Json.Serialization.JsonConstructor]
    public TemplateFolderDefinition(TemplateFolderId id, string name, string key, TemplateFolderId? parentId)
    {
        if (id.Value == Guid.Empty) throw new ArgumentException("Folder ID is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Folder name is required.");
        AuthoringLimits.Check(name, AuthoringLimits.Name, nameof(name));
        Id = id;
        Name = name.Trim();
        Key = new ContentItemKey(key).ToString();
        ParentId = parentId;
    }
    public TemplateFolderId Id { get; }
    public override string Name { get; }
    public string Key { get; }
    public TemplateFolderId? ParentId { get; }
    public override TemplateId TemplateId => SystemTemplateIds.TemplateFolder;
}
