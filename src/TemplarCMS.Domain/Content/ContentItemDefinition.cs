namespace TemplarCMS.Domain.Content;

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
        ContentItemId id,
        string name,
        ContentItemKey key,
        TemplateId templateId,
        ContentItemId? parentId = null,
        string? icon = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Content item name is required.", nameof(name));
        }

        if (parentId == id)
        {
            throw new ArgumentException(
                "Content item cannot be its own parent.",
                nameof(parentId));
        }

        Id = id;
        Name = name.Trim();
        Key = key;
        TemplateId = templateId;
        ParentId = parentId;
        Icon = AuthoringIconCatalog.Normalize(icon);
    }

    /// <summary>
    /// Gets the stable content item identifier.
    /// </summary>
    public ContentItemId Id { get; }

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
    public TemplateId TemplateId { get; }

    /// <summary>
    /// Gets the optional parent content item identifier.
    /// </summary>
    public ContentItemId? ParentId { get; }

    /// <summary>Gets the optional icon override for this item.</summary>
    public string? Icon { get; }

    /// <summary>
    /// Gets a value indicating whether the item is a root item.
    /// </summary>
    public bool IsRoot => ParentId == null;

    /// <summary>
    /// Gets a value indicating whether the item has a parent item.
    /// </summary>
    public bool HasParent => ParentId != null;

    /// <summary>
    /// Gets a value indicating whether the item uses the supplied template.
    /// </summary>
    public bool UsesTemplate(TemplateId templateId)
    {
        return TemplateId == templateId;
    }

    /// <summary>
    /// Gets a value indicating whether the item is a direct child of the supplied parent.
    /// </summary>
    public bool IsDirectChildOf(ContentItemId parentId)
    {
        return ParentId == parentId;
    }

    /// <summary>
    /// Returns a copy of the item with updated authored metadata.
    /// </summary>
    public ContentItemDefinition UpdateMetadata(string name)
    {
        return new ContentItemDefinition(
            Id,
            name,
            Key,
            TemplateId,
            ParentId,
            Icon);
    }

    /// <summary>
    /// Returns a copy of the item with a renamed content key and display name.
    /// </summary>
    public ContentItemDefinition Rename(
        string name,
        ContentItemKey key)
    {
        return new ContentItemDefinition(
            Id,
            name,
            key,
            TemplateId,
            ParentId,
            Icon);
    }

    /// <summary>
    /// Returns a copy of the item moved beneath a new parent.
    /// </summary>
    public ContentItemDefinition MoveTo(ContentItemId? parentId)
    {
        return new ContentItemDefinition(
            Id,
            Name,
            Key,
            TemplateId,
            parentId,
            Icon);
    }

    /// <summary>
    /// Computes the canonical content path for the item.
    /// </summary>
    /// <param name="parentPath">
    /// The canonical parent path for child items. Omit for root items.
    /// </param>
    public ContentPath GetPath(ContentPath? parentPath = null)
    {
        if (IsRoot)
        {
            if (parentPath != null)
            {
                throw new InvalidOperationException(
                    $"Root content item '{Id}' cannot be resolved from a parent path.");
            }

            return ContentPath.FromRoot(Key);
        }

        if (parentPath == null)
        {
            throw new InvalidOperationException(
                $"Parent path is required to resolve content item '{Id}'.");
        }

        return ContentPath.Append(
            parentPath.Value,
            Key);
    }
}
