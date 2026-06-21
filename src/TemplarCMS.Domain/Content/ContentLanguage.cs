namespace TemplarCMS.Domain.Content;

/// <summary>
/// Represents a normalized content language name.
/// </summary>
public readonly record struct ContentLanguage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContentLanguage" /> struct.
    /// </summary>
    /// <param name="name">The language name.</param>
    public ContentLanguage(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Content language is required.", nameof(name));
        }

        Name = name.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Gets the normalized language name.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }
}
