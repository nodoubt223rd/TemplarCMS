namespace TemplarCMS.Domain.Content;

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

        if (string.IsNullOrWhiteSpace(Value))
        {
            throw new ArgumentException("Content item key is required.", nameof(value));
        }
    }

    /// <summary>
    /// Creates a content item key from an authored display name using SEO-friendly normalization.
    /// </summary>
    public static ContentItemKey FromDisplayName(string value)
    {
        return new ContentItemKey(value);
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
        var builder = new System.Text.StringBuilder();
        var previousWasSeparator = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
                continue;
            }

            if (character is '\'' or '’')
            {
                continue;
            }

            if (builder.Length > 0 && !previousWasSeparator)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        if (builder.Length > 0 && builder[^1] == '-')
        {
            builder.Length--;
        }

        return builder.ToString();
    }
}
