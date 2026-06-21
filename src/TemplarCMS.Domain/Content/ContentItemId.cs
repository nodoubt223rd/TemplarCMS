namespace TemplarCMS.Domain.Content;

/// <summary>
/// Represents a stable content item identifier.
/// </summary>
public readonly record struct ContentItemId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentItemId" /> struct.
    /// </summary>
    /// <param name="value">The content item identifier value.</param>
    public ContentItemId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Content item id is required.",
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
