namespace TemplarCMS.Persistence.Content;

/// <summary>
/// Represents a persisted field value row for a content item.
/// </summary>
public sealed class PersistenceContentFieldValue
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }

    public Guid FieldId { get; set; }

    public string FieldKey { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public int Version { get; set; }

    public string? Value { get; set; }

    public PersistenceContentItem? Item { get; set; }
}
