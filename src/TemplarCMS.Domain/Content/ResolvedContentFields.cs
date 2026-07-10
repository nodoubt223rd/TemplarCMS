namespace TemplarCMS.Domain.Content;

/// <summary>
/// Represents resolved field values for a content item.
/// </summary>
public sealed class ResolvedContentFields
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResolvedContentFields" /> class.
    /// </summary>
    /// <param name="fields">The resolved field values.</param>
    /// <param name="convertedFields">The resolved typed field values.</param>
    public ResolvedContentFields(
        IReadOnlyDictionary<string, ContentFieldValue?> fields,
        IReadOnlyDictionary<string, TypedFieldValue> convertedFields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(convertedFields);

        Fields = fields;
        ConvertedFields = convertedFields;
    }

    /// <summary>
    /// Gets the resolved field values.
    /// </summary>
    public IReadOnlyDictionary<string, ContentFieldValue?> Fields { get; }

    /// <summary>
    /// Gets the resolved typed field values.
    /// </summary>
    public IReadOnlyDictionary<string, TypedFieldValue> ConvertedFields { get; }
}
