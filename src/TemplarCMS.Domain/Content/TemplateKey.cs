namespace TemplarCMS.Domain.Content;

/// <summary>
/// Represents a stable template key used for lookup.
/// </summary>
public readonly record struct TemplateKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateKey" /> struct.
    /// </summary>
    /// <param name="value">The template key value.</param>
    public TemplateKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Template key is required.", nameof(value));
        }

        Value = value.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Gets the canonical template key value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }
}
