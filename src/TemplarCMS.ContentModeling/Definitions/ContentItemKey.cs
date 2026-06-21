namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Represents a stable content item key used for sibling lookup.
/// </summary>
public readonly record struct ContentItemKey
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentItemKey" /> struct.
    /// </summary>
    /// <param name="value">The content item key value.</param>
    public ContentItemKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Content item key is required.", nameof(value));
        }

        Value = Normalize(value);
    }

    /// <summary>
    /// Gets the canonical content item key value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }

    private static string Normalize(string value)
    {
        var segments =
            value.Trim()
                .ToLowerInvariant()
                .Split(
                    null as char[],
                    StringSplitOptions.RemoveEmptyEntries);

        return string.Join("-", segments);
    }
}
