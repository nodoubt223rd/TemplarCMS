namespace TemplarCMS.Domain.Content;

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
    /// <param name="convertedFields">
    /// The resolved typed field values.
    /// </param>
    public ResolvedContentItem(
        ContentItemDefinition item,
        IReadOnlyDictionary<string, ContentFieldValue?> fields,
        IReadOnlyDictionary<string, TypedFieldValue> convertedFields)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(convertedFields);

        Item = item;
        Fields = fields;
        ConvertedFields = convertedFields;
    }

    /// <summary>
    /// Gets the content item.
    /// </summary>
    public ContentItemDefinition Item { get; }

    /// <summary>
    /// Gets the resolved field values.
    /// </summary>
    public IReadOnlyDictionary<string, ContentFieldValue?> Fields { get; }

    /// <summary>
    /// Gets the resolved typed field values.
    /// </summary>
    public IReadOnlyDictionary<string, TypedFieldValue> ConvertedFields { get; }
}
