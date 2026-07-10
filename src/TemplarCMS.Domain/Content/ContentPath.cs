namespace TemplarCMS.Domain.Content;

/// <summary>
/// Represents a computed absolute content path.
/// </summary>
public readonly record struct ContentPath
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentPath" /> struct.
    /// </summary>
    /// <param name="value">The absolute path value.</param>
    public ContentPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Content path is required.", nameof(value));
        }

        var normalized =
            Normalize(value);

        if (normalized.Length == 0)
        {
            throw new ArgumentException("Content path is required.", nameof(value));
        }

        Value = normalized;
    }

    /// <summary>
    /// Gets the normalized absolute path value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a root path for a content item key.
    /// </summary>
    public static ContentPath FromRoot(ContentItemKey key)
    {
        return new ContentPath($"/{key}");
    }

    /// <summary>
    /// Appends a child content item key to a parent path.
    /// </summary>
    public static ContentPath Append(
        ContentPath parentPath,
        ContentItemKey key)
    {
        return new ContentPath($"{parentPath.Value}/{key}");
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value;
    }

    private static string Normalize(string value)
    {
        var segments =
            value.Trim()
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Length == 0
            ? string.Empty
            : "/" + string.Join("/", segments);
    }
}
