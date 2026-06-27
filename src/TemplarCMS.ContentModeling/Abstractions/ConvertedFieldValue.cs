using TemplarCMS.Domain.Content;
using TemplarCMS.ContentModeling.Definitions;

namespace TemplarCMS.ContentModeling.Abstractions;

/// <summary>
/// Represents a stored field value converted into a typed runtime value.
/// </summary>
public sealed class ConvertedFieldValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConvertedFieldValue" /> class.
    /// </summary>
    /// <param name="field">The field definition that describes the value.</param>
    /// <param name="source">The stored source value, if present.</param>
    /// <param name="value">The converted runtime value.</param>
    public ConvertedFieldValue(
        FieldDefinition field,
        ContentFieldValue? source,
        TypedFieldValue value)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(value);

        Field = field;
        Source = source;
        Value = value;
    }

    /// <summary>
    /// Gets the field definition that describes the value.
    /// </summary>
    public FieldDefinition Field { get; }

    /// <summary>
    /// Gets the stored source value, if present.
    /// </summary>
    public ContentFieldValue? Source { get; }

    /// <summary>
    /// Gets the converted runtime value.
    /// </summary>
    public TypedFieldValue Value { get; }
}
