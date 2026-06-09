namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Represents a stored content value for a field on a content item.
/// </summary>
public sealed class ContentFieldValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentFieldValue" /> class.
    /// </summary>
    /// <param name="itemId">The content item that owns the value.</param>
    /// <param name="fieldId">The field definition identifier.</param>
    /// <param name="fieldKey">The field definition key.</param>
    /// <param name="language">The content language for the value.</param>
    /// <param name="version">The content version for the value.</param>
    /// <param name="value">The stored field value.</param>
    public ContentFieldValue(
        Guid itemId,
        Guid fieldId,
        string fieldKey,
        ContentLanguage language,
        ContentVersion version,
        string? value)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("Content item id is required.", nameof(itemId));
        }

        if (fieldId == Guid.Empty)
        {
            throw new ArgumentException("Field id is required.", nameof(fieldId));
        }

        if (string.IsNullOrWhiteSpace(fieldKey))
        {
            throw new ArgumentException("Field key is required.", nameof(fieldKey));
        }

        ItemId = itemId;
        FieldId = fieldId;
        FieldKey = fieldKey.Trim();
        Language = language;
        Version = version;
        Value = value;
    }

    /// <summary>
    /// Gets the content item that owns the value.
    /// </summary>
    public Guid ItemId { get; }

    /// <summary>
    /// Gets the field definition identifier.
    /// </summary>
    public Guid FieldId { get; }

    /// <summary>
    /// Gets the field definition key.
    /// </summary>
    public string FieldKey { get; }

    /// <summary>
    /// Gets the content language for the value.
    /// </summary>
    public ContentLanguage Language { get; }

    /// <summary>
    /// Gets the content version for the value.
    /// </summary>
    public ContentVersion Version { get; }

    /// <summary>
    /// Gets the stored field value.
    /// </summary>
    public string? Value { get; }
}
