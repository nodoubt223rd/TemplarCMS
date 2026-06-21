namespace TemplarCMS.Domain.Content;

/// <summary>
/// Represents a stable template identifier.
/// </summary>
public readonly record struct TemplateId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateId" /> struct.
    /// </summary>
    /// <param name="value">The template identifier value.</param>
    public TemplateId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Template id is required.",
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
