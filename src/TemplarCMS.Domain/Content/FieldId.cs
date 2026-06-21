namespace TemplarCMS.Domain.Content;

/// <summary>
/// Represents a stable field identifier.
/// </summary>
public readonly record struct FieldId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldId" /> struct.
    /// </summary>
    /// <param name="value">The field identifier value.</param>
    public FieldId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Field id is required.",
                nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the identifier value.
    /// </summary>
    public Guid Value { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
