namespace TemplarCMS.Persistence.Content;

/// <summary>
/// Represents a persisted content item row.
/// </summary>
public sealed class PersistenceContentItem
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public Guid TemplateId { get; set; }

    public Guid? ParentId { get; set; }

    public string? Icon { get; set; }

    public List<PersistenceContentFieldValue> FieldValues { get; set; } = new();
}
