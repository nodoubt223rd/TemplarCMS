namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Represents a content item within the content tree.
/// </summary>
public sealed class ContentItemDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentItemDefinition" /> class.
    /// </summary>
    /// <param name="id">The stable content item identifier.</param>
    /// <param name="name">The display name of the content item.</param>
    /// <param name="key">The stable key used to identify the content item among siblings.</param>
    /// <param name="templateId">The template used by the content item.</param>
    /// <param name="parentId">The optional parent content item identifier.</param>
    public ContentItemDefinition(
        Guid id,
        string name,
        ContentItemKey key,
        Guid templateId,
        Guid? parentId = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Content item id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Content item name is required.", nameof(name));
        }

        if (templateId == Guid.Empty)
        {
            throw new ArgumentException("Content item template id is required.", nameof(templateId));
        }

        Id = id;
        Name = name.Trim();
        Key = key;
        TemplateId = templateId;
        ParentId = parentId;
    }

    /// <summary>
    /// Gets the stable content item identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the display name of the content item.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the stable key used to identify the content item among siblings.
    /// </summary>
    public ContentItemKey Key { get; }

    /// <summary>
    /// Gets the template used by the content item.
    /// </summary>
    public Guid TemplateId { get; }

    /// <summary>
    /// Gets the optional parent content item identifier.
    /// </summary>
    public Guid? ParentId { get; }
}
