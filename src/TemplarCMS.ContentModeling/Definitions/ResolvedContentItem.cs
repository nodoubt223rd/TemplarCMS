namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Represents a resolved content item.
/// </summary>
public sealed class ResolvedContentItem
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ResolvedContentItem"/> class.
    /// </summary>
    /// <param name="item">
    /// The resolved content item.
    /// </param>
    /// <param name="fields">
    /// The resolved field values.
    /// </param>
    public ResolvedContentItem(
        ContentItemDefinition item,
        IReadOnlyDictionary<string, ContentFieldValue?> fields)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(fields);

        Item = item;
        Fields = fields;
    }

    /// <summary>
    /// Gets the content item.
    /// </summary>
    public ContentItemDefinition Item { get; }

    /// <summary>
    /// Gets the resolved field values.
    /// </summary>
    public IReadOnlyDictionary<string, ContentFieldValue?> Fields { get; }
}
